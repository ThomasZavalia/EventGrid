using DotNet.Testcontainers.Builders;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace BookingService.IntegrationTests;


[Trait("Category", "Redis")]
public class RedisAtomicityTests : IAsyncLifetime
{
   

    private RedisContainer _redisContainer = null!;
    private IConnectionMultiplexer _connection = null!;
    private IDatabase _db = null!;

   
    private const string TOTAL_TICKETS_KEY = "queue:total_tickets";
    private const string TICKET_TTL_SECONDS = "3600";
    private const string USER_TICKET_PREFIX = "queue:user:";

    
    public async Task InitializeAsync()
    {
        
        _redisContainer = new RedisBuilder()
            .WithImage("redis:alpine")
            .Build();

        await _redisContainer.StartAsync();

        
        _connection = await ConnectionMultiplexer.ConnectAsync(
            _redisContainer.GetConnectionString()
        );
        _db = _connection.GetDatabase();
    }

   
    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

   
    [Fact]
    public async Task INCR_Concurrente_NuncaGeneraTicketsDuplicados()
    {
        // ARRANGE
        const int USUARIOS_CONCURRENTES = 100;

        // ACT
        var tareas = Enumerable.Range(0, USUARIOS_CONCURRENTES)
            .Select(_ => _db.StringIncrementAsync(TOTAL_TICKETS_KEY))
            .ToList();

       
        var tickets = await Task.WhenAll(tareas);

        // ASSERT
        Assert.Equal(USUARIOS_CONCURRENTES, tickets.Length);

       
        var ticketsUnicos = tickets.ToHashSet();
        Assert.Equal(USUARIOS_CONCURRENTES, ticketsUnicos.Count);

       
        Assert.Equal(USUARIOS_CONCURRENTES, tickets.Max());
        Assert.Equal(1, tickets.Min());
    }

  
    [Fact]
    public async Task JoinQueue_UsuarioDuplicado_RecuperaTicketExistenteSinIncrementar()
    {
        // ARRANGE
        var userId = Guid.NewGuid().ToString();
        var userTicketKey = $"{USER_TICKET_PREFIX}{userId}";

        // ACT

        
        var ticketExistente = await _db.StringGetAsync(userTicketKey);
        long ticketNumero;

        if (ticketExistente.IsNullOrEmpty)
        {
           
            ticketNumero = await _db.StringIncrementAsync(TOTAL_TICKETS_KEY);
           
            await _db.StringSetAsync(
                userTicketKey,
                ticketNumero.ToString(),
                TimeSpan.FromSeconds(int.Parse(TICKET_TTL_SECONDS))
            );
        }
        else
        {
            ticketNumero = long.Parse(ticketExistente!);
        }

        var totalDespuesDePrimerIntent = await _db.StringGetAsync(TOTAL_TICKETS_KEY);

        // ACT
        var ticketExistente2 = await _db.StringGetAsync(userTicketKey);
        long ticketNumero2;

        if (ticketExistente2.IsNullOrEmpty)
        {
            ticketNumero2 = await _db.StringIncrementAsync(TOTAL_TICKETS_KEY);
            await _db.StringSetAsync(userTicketKey, ticketNumero2.ToString(),
                TimeSpan.FromSeconds(int.Parse(TICKET_TTL_SECONDS)));
        }
        else
        {
           
            ticketNumero2 = long.Parse(ticketExistente2!);
        }

        var totalDespuesDeSundoIntento = await _db.StringGetAsync(TOTAL_TICKETS_KEY);

        // ASSERT

      
        Assert.Equal(ticketNumero, ticketNumero2);

       
        Assert.Equal(totalDespuesDePrimerIntent, totalDespuesDeSundoIntento);
        Assert.Equal("1", totalDespuesDeSundoIntento.ToString());

       
        var ttl = await _db.KeyTimeToLiveAsync(userTicketKey);
        Assert.NotNull(ttl);
        Assert.True(ttl.Value.TotalSeconds > 0, "El ticket deberia tener TTL para auto-expirar");
    }

   

  
    [Fact]
    public async Task DistributedLock_SetNX_SoloUnProcesoAdquiereLockSimultaneo()
    {
        // ARRANGE
        const string LOCK_KEY = "lock:queue_worker";
        const string LOCK_VALUE_PROCESO_A = "proceso-A";
        const string LOCK_VALUE_PROCESO_B = "proceso-B";

        // ACT

       
        var procesoAGano = await _db.StringSetAsync(
            LOCK_KEY,
            LOCK_VALUE_PROCESO_A,
            TimeSpan.FromMilliseconds(2000),
            When.NotExists
        );

        var procesoBGano = await _db.StringSetAsync(
            LOCK_KEY,
            LOCK_VALUE_PROCESO_B,
            TimeSpan.FromMilliseconds(2000),
            When.NotExists
        );

        // ASSERT

       
        Assert.True(procesoAGano, "El Proceso A debería haber ganado el lock.");
        Assert.False(procesoBGano, "El Proceso B NO debería poder adquirir el lock mientras A lo tiene.");

        var valorActual = await _db.StringGetAsync(LOCK_KEY);
        Assert.Equal(LOCK_VALUE_PROCESO_A, valorActual.ToString());

        
        const string luaScript = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
        ";

      
        var procesoBLibero = (int)await _db.ScriptEvaluateAsync(
            luaScript,
            new RedisKey[] { LOCK_KEY },
            new RedisValue[] { LOCK_VALUE_PROCESO_B }
        );
        Assert.Equal(0, procesoBLibero); 

       
        var procesoALibero = (int)await _db.ScriptEvaluateAsync(
            luaScript,
            new RedisKey[] { LOCK_KEY },
            new RedisValue[] { LOCK_VALUE_PROCESO_A }
        );
        Assert.Equal(1, procesoALibero); 

       
        var procesoBGanoAhora = await _db.StringSetAsync(
            LOCK_KEY,
            LOCK_VALUE_PROCESO_B,
            TimeSpan.FromMilliseconds(2000),
            When.NotExists
        );
        Assert.True(procesoBGanoAhora,
            "El Proceso B deberia poder adquirir el lock después de que A lo libero");
    }
}
