
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';



const BASE_URL = 'http://127.0.0.1:8090';


const TARGET_SEAT_ID = __ENV.SEAT_ID || '00000000-0000-0000-0000-000000000000';

const BASE_LOGIN_TOKEN = __ENV.LOGIN_TOKEN || 'tu-token-aqui';

const queueTokensObtenidos = new Counter('queue_tokens_obtenidos');

const reservasExitosas = new Counter('reservas_exitosas');

const conflictos409 = new Counter('conflictos_409');

const errores500 = new Counter('errores_500_criticos');

const latenciaReserva = new Trend('latencia_reserva_ms', true);



export const options = {
  scenarios: {
    smoke_test: {
      executor: 'constant-vus',
      vus: 1,
      duration: '10s',
      tags: { test_type: 'smoke' },
      gracefulStop: '5s',
    },


    load_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 10 },  
        { duration: '20s', target: 30 },  
        { duration: '10s', target: 0 },   
      ],
      startTime: '15s', 
      tags: { test_type: 'load' },
      gracefulStop: '5s',
    },

    spike_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2s',  target: 50 }, 
        { duration: '10s', target: 50 }, 
        { duration: '2s',  target: 0  }, 
      ],
      startTime: '75s', 
      tags: { test_type: 'spike' },
      gracefulStop: '5s',
    },
  },


  thresholds: {
  
    'http_req_failed': ['rate<0.02'],

  
    'http_req_duration': ['p(95)<2000'],

  
    'checks': ['rate>0.95'],

  
    'errores_500_criticos': ['count<1'],

    
    'latencia_reserva_ms': ['p(95)<3000'],
  },
};


export default function () {
  
  const userId = generateUUID();

  
  const joinPayload = JSON.stringify({ userId: userId });
  const joinParams = {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'join_queue' },
  };

  const joinRes = http.post(`${BASE_URL}/api/queue/join`, joinPayload, joinParams);

 
  const joinOk = check(joinRes, {
    'joinQueue: status es 200': (r) => r.status === 200,
    'joinQueue: tiene ticketNumber': (r) => {
      try { return JSON.parse(r.body).ticketNumber > 0; }
      catch { return false; }
    },
  });

  if (!joinOk || joinRes.status !== 200) {
   
    sleep(0.5);
    return;
  }

  let joinData = JSON.parse(joinRes.body);
  let status = joinData.status;
  let accessToken = joinData.accessToken;

  let pollAttempts = 0;
  while (status === 'WAITING' && pollAttempts < 60) {
    check(joinRes, {
      'joinQueue: en cola (WAITING es válido)': () => status === 'WAITING',
    });
    
    sleep(1); 
    pollAttempts++;
    
    let pollRes = http.post(`${BASE_URL}/api/queue/join`, joinPayload, joinParams);
    if (pollRes.status === 200) {
        joinData = JSON.parse(pollRes.body);
        status = joinData.status;
        accessToken = joinData.accessToken;
    } else {
        break;
    }
  }

  if (status !== 'PROCESSED') {
   
    return;
  }

 
  if (accessToken) {
    queueTokensObtenidos.add(1);
  }

 

  if (!accessToken) {
    sleep(0.1);
    return;
  }

  const reservePayload = JSON.stringify({ seatId: TARGET_SEAT_ID });
  const reserveParams = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`,
    },
    tags: { endpoint: 'reserve_seat' },
  };

  const startTime = Date.now();
  const reserveRes = http.post(`${BASE_URL}/api/bookings/reserve`, reservePayload, reserveParams);
  latenciaReserva.add(Date.now() - startTime);

 
  check(reserveRes, {
    
    'reserve: NO hay error 500': (r) => r.status !== 500,

   
    'reserve: NO hay error 400': (r) => r.status !== 400,

 
    'reserve: respuesta es 200 o 409': (r) => r.status === 200 || r.status === 409,
  });

 
  if (reserveRes.status === 200) {
    reservasExitosas.add(1);
    check(reserveRes, {
      'reserve (éxito): tiene mensaje de confirmación': (r) => {
        try { return JSON.parse(r.body).message !== undefined; }
        catch { return false; }
      },
    });
  } else if (reserveRes.status === 409) {
    conflictos409.add(1);
  } else if (reserveRes.status === 500) {
    errores500.add(1);
   
    console.error(`ERROR 500 en reserva: ${reserveRes.body}`);
  }

 
  sleep(Math.random() * 0.5 + 0.1); 
}


export function handleSummary(data) {
  const reservasTotal = data.metrics.reservas_exitosas
    ? data.metrics.reservas_exitosas.values.count
    : 0;

  const errores500Total = data.metrics.errores_500_criticos
    ? data.metrics.errores_500_criticos.values.count
    : 0;

  const conflictos = data.metrics.conflictos_409
    ? data.metrics.conflictos_409.values.count
    : 0;

  console.log('ANALISIS DE RACE CONDITIONS — EventGrid ');
  console.log(`Reservas exitosas (solo 1 válida):  ${String(reservasTotal).padStart(4)} `);
  console.log(`Conflictos 409 (comportamiento OK): ${String(conflictos).padStart(4)} `);
  console.log(`Errores 500 críticos (debe ser 0):  ${String(errores500Total).padStart(4)}`);

  if (errores500Total > 0) {
    console.log('  ❌ FALLA CRÍTICA: Hubo errores 500. Bug de concurren');
    console.log('     cia detectado. Revisar logs del BookingService');
  } else if (reservasTotal > 1) {
    console.log('  ❌ FALLA CRÍTICA: Más de 1 reserva exitosa detectada');
    console.log('     SOBREVENTA confirmada. Race condition en la BD');
  } else if (reservasTotal === 1) {
    console.log('  ✅ SISTEMA ROBUSTO: Solo 1 reserva exitosa');
    console.log('     No se detectaron race conditions');
  } else {
    console.log('ADVERTENCIA: Nadie pudo completar la reserva');
   
  }

  
  return {
    stdout: JSON.stringify(data, null, 2),
  };
}


function generateUUID() {
  const chars = '0123456789abcdef';
  let uuid = '';
  for (let i = 0; i < 36; i++) {
    if (i === 8 || i === 13 || i === 18 || i === 23) {
      uuid += '-';
    } else if (i === 14) {
      uuid += '4';
    } else if (i === 19) {
      uuid += chars[(Math.random() * 4) | 8];
    } else {
      uuid += chars[(Math.random() * 16) | 0];
    }
  }
  return uuid;
}
