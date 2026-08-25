# EventGrid

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![Node.js](https://img.shields.io/badge/Node.js-20-339933?style=flat&logo=node.js)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?style=flat&logo=redis)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Broker-FF6600?style=flat&logo=rabbitmq)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-DB-4169E1?style=flat&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker)

Sistema distribuido de reserva de entradas diseñado para soportar alta concurrencia mediante una **Cola Virtual**.

La arquitectura separa responsabilidades en microservicios: Node.js y Redis manejan la fila de acceso, mientras que .NET 10 con Entity Framework y MassTransit garantiza la consistencia transaccional de las reservas y los pagos.

---

## Arquitectura

```mermaid
graph TD
    User([Usuario]) --> Gateway[API Gateway / YARP]
    
    Gateway -->|/api/queue| VQS[Virtual Queue Service<br/>Node.js + TS]
    Gateway -->|/api/auth & /bookings| BS[Booking Service<br/>.NET 10]
    
    VQS <-->|Estado Cola| Redis[(Redis)]
    VQS -->|gRPC GetQueueToken| BS
    
    BS <-->|Transacciones| Postgres[(PostgreSQL)]
    BS -->|Publish PaymentEvent| RMQ[RabbitMQ]
    
    Worker[Payment Consumer<br/>.NET 10] <-->|Consume| RMQ
    Worker <-->|Actualiza Seat| Postgres
```

### Componentes

- **API Gateway (YARP)**: Punto de entrada único. Enruta `/api/queue` al VirtualQueueService y `/api/auth`, `/api/bookings` al BookingService.
- **Virtual Queue Service (Node.js + TypeScript)**: Gestiona el acceso al sistema limitando la concurrencia. Emite tickets numerados en Redis y devuelve un *QueuePass JWT* (via gRPC al BookingService) cuando es el turno del usuario.
- **Booking Service (.NET 10)**: Emite JWTs de identidad, valida el QueuePass y orquesta la reserva. Publica eventos de pago a RabbitMQ para procesamiento asíncrono.
- **Payment Consumer (.NET 10)**: Worker que consume eventos de pago de RabbitMQ y actualiza el estado final del asiento en PostgreSQL.

---

## Instalación

### Requisitos

- Docker y Docker Compose

### Variables de entorno

Crear un archivo `.env` en la raíz del proyecto con el siguiente contenido:

```env
POSTGRES_USER=admin
POSTGRES_PASSWORD=password123
JWT_SECRET=una-clave-secreta-larga-y-segura
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
```

### Levantar el entorno

```bash
git clone https://github.com/ThomasZavalia/EventGrid.git
cd EventGrid
docker compose up -d --build
```

El API Gateway queda disponible en `http://localhost:8090`.

Alternativamente, el script `dev-setup.sh` automatiza este proceso, espera a que todos los servicios estén listos y corre el seed inicial:

```bash
bash scripts/dev-setup.sh
```

---

## Uso (API REST)

```bash
# 1. Registrar usuario
curl -X POST http://localhost:8090/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test123!","firstName":"Juan","lastName":"Perez"}'

# 2. Login — guardar el "id" y el "token" de la respuesta
curl -X POST http://localhost:8090/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test123!"}'

# 3. Inicializar datos de prueba
curl -X POST http://localhost:8090/api/admin/seed \
  -H "Authorization: Bearer <LOGIN_TOKEN>"

# 4. Unirse a la Cola Virtual
curl -X POST http://localhost:8090/api/queue/join \
  -H "Content-Type: application/json" \
  -d '{"userId":"<TU_USER_ID>"}'
# Si es tu turno, la respuesta incluye un "accessToken" (QueuePass)

# 5. Reservar asiento (requiere el QueuePass, no el login token)
curl -X POST http://localhost:8090/api/bookings/reserve \
  -H "Authorization: Bearer <QUEUE_PASS>" \
  -H "Content-Type: application/json" \
  -d '{"seatId":"<SEAT_ID>"}'

# 6. Confirmar pago (procesamiento asíncrono via RabbitMQ)
curl -X POST http://localhost:8090/api/bookings/confirm-payment \
  -H "Authorization: Bearer <QUEUE_PASS>" \
  -H "Content-Type: application/json" \
  -d '{"seatId":"<SEAT_ID>"}'
```

---

## Testing

El proyecto incluye tres niveles de pruebas automatizadas.

### Tests de integración (C# / xUnit)

Cubren dos áreas críticas:

**Flujo asíncrono con RabbitMQ** (`PaymentFlowIntegrationTests.cs`): usa el InMemory Test Harness de MassTransit para verificar que los eventos de pago se publican y consumen correctamente sin depender de un broker externo.

**Concurrencia en Redis** (`RedisAtomicityTests.cs`): usa Testcontainers para levantar una instancia real de Redis durante el test y valida que las operaciones de incremento atómico y los distributed locks con Lua scripts se comporten correctamente bajo 50 threads paralelos.

```bash
dotnet test tests/BookingService.IntegrationTests
```

### Load test (k6)

Simula tres escenarios de carga (Smoke, Load, Spike) con hasta 50 usuarios virtuales concurrentes intentando reservar el mismo asiento. El objetivo es detectar race conditions (sobreventa).

```bash
k6 run --env SEAT_ID="<uuid-del-asiento>" tests/stress-test/k6-load-test.js
```

Resultado esperado: una sola reserva exitosa, el resto recibe `409 Conflict`, cero errores 500.

El script `run-tests.sh` ejecuta ambas suites en secuencia y muestra un resumen final:

```bash
bash scripts/run-tests.sh <seat-uuid>
```

---

## Scripts

| Script | Descripción |
|---|---|
| `scripts/dev-setup.sh` | Levanta Docker, espera que el gateway esté disponible y corre el seed |
| `scripts/check-services.sh` | Verifica el estado de cada servicio del stack |
| `scripts/run-tests.sh [seat-uuid]` | Corre los tests de integración C# y opcionalmente el load test de k6 |

---

## Seguridad

- **Segregación de tokens JWT**: El token de login no tiene permisos de reserva. Solo el token emitido por la cola virtual (`is_queue_pass: true`) permite acceder a `/api/bookings`. Esto evita que un usuario saltee la fila accediendo directamente al endpoint de reserva.
- **CORS**: Diferenciado entre entornos (`AllowAll` en Development, `AllowFrontend` en Production).
- **Variables de entorno**: Todas las credenciales críticas (Postgres, RabbitMQ, JWT Secret) se inyectan por variables de entorno, sin hardcodeo en el código.

---

## Observabilidad

El sistema incluye trazas distribuidas con OpenTelemetry exportadas a Jaeger. Cubre automáticamente requests HTTP, consultas a la base de datos y mensajes de RabbitMQ.

Con el sistema corriendo, abrí `http://localhost:16686` en el navegador. Desde el selector de servicio podés elegir `BookingService` y ver una traza completa por cada operación: desde el request al Gateway, pasando por la query a PostgreSQL, hasta la publicación del evento en RabbitMQ y su consumo por el Worker.

El endpoint de health check del BookingService está disponible en `http://localhost:8090/api/bookings/health` (vía Gateway) y devuelve el estado del servicio y la conectividad con la base de datos.

---

## Limitaciones conocidas

El load test reveló un comportamiento a tener en cuenta: si muchos usuarios se unen a la cola y luego abandonan la sesión (sin llegar a reservar), sus tickets quedan pendientes en Redis. El Queue Worker los procesa de todas formas, generando una brecha entre el contador interno y los usuarios reales activos. Bajo tráfico muy alto esto puede ralentizar el tiempo de espera para usuarios genuinos.

La solución diseñada para una próxima versión es reemplazar el contador simple por un Redis Sorted Set combinado con un mecanismo de heartbeat: los clientes indican periódicamente que siguen activos, y el worker descarta automáticamente los tickets sin actividad reciente.

---

## Roadmap

- [ ] Heartbeat + Redis ZSET para limpiar tickets abandonados de la cola
- [ ] Circuit Breaker en el Virtual Queue Service (fail-fast si el BookingService no responde)
- [ ] mTLS en el canal gRPC interno entre Node.js y .NET
