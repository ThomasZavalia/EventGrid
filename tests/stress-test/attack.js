const axios = require('axios');
const chalk = require('chalk'); 
const crypto = require('crypto');


const TOTAL_BOTS = 50; 
const SEAT_ID_TARGET = "09dc2103-7240-421b-883d-73233427c062"; 
const QUEUE_URL = 'http://localhost:3000/api/queue';
const BOOKING_URL = 'http://localhost:5000/api/bookings';


let results = {
    success: 0,
    conflict: 0, 
    failed: 0,   
    queue_errors: 0
};


async function runBot(botId) {
   const userId = crypto.randomUUID();
    const logPrefix = `[Bot ${botId}]`;

    try {
        
        let queueResponse = await axios.post(`${QUEUE_URL}/join`, { userId });
        let { ticketNumber, status, accessToken } = queueResponse.data;

        
        while (status === 'WAITING') {
           
            await new Promise(r => setTimeout(r, 1000)); 
            
          
            queueResponse = await axios.post(`${QUEUE_URL}/join`, { userId });
            status = queueResponse.data.status;
            accessToken = queueResponse.data.accessToken;
        }

        if (!accessToken) {
            console.log(chalk.red(`${logPrefix}  Salió de la cola pero sin token!`));
            results.queue_errors++;
            return;
        }

        
        console.log(chalk.yellow(`${logPrefix}  Token obtenido. Intentando comprar...`));
        
        try {
            await axios.post(`${BOOKING_URL}/reserve`, 
                { seatId: SEAT_ID_TARGET },
                { headers: { Authorization: `Bearer ${accessToken}` } }
            );
            
            console.log(chalk.green.bold(`${logPrefix}  COMPRA EXITOSA!`));
            results.success++;

        } catch (error) {
        
        if (error.response) {
            
            if (error.response.status === 409) {
                console.log(chalk.blue(`${logPrefix}  Perdió: Asiento ocupado (409)`));
                results.conflict++;

            } else if (error.response.status === 404) {
                console.log(chalk.red(`${logPrefix}  Error: Asiento no encontrado (404)`));
                results.failed++;

            } else {
                
                const errorMsg = JSON.stringify(error.response.data);
                const statusCode = error.response.status;

                console.log(chalk.magenta(`${logPrefix}  INVESTIGAR: Status: ${statusCode} | Mensaje: ${errorMsg}`));
                results.failed++;
            }

        } else {
           
            console.log(chalk.red(`${logPrefix}  Error de RED (Sin respuesta): ${error.message}`));
            results.queue_errors++;
        }
    }

    } catch (error) {
        console.log(chalk.red(`${logPrefix} Murió en la cola: ${error.message}`));
        results.queue_errors++;
    }
}

(async () => {
    console.log(chalk.cyan(` INICIANDO ATAQUE CON ${TOTAL_BOTS} BOTS...`));
    console.log(chalk.cyan(` Objetivo: Asiento ${SEAT_ID_TARGET}`));

 
    const promises = [];
    for (let i = 1; i <= TOTAL_BOTS; i++) {
        promises.push(runBot(i));
        
        await new Promise(r => setTimeout(r, Math.random() * 50));
    }

    await Promise.all(promises);

    console.log('\n' + chalk.bgBlue.white(' RESUMEN FINAL '));
    console.log(chalk.green(`✅ Compras Exitosas: ${results.success}`));
    console.log(chalk.blue(` Conflictos (409): ${results.conflict}`));
    console.log(chalk.red(`❌ Fallos/Errores:   ${results.failed + results.queue_errors}`));
    
    if (results.success > 1) {
        console.log(chalk.bgRed.white('  GRAVE: SOBREVENTA DETECTADA (Mas de 1 exito) '));
    } else if (results.success === 1) {
        console.log(chalk.bgGreen.black('  SISTEMA ROBUSTO: Solo 1 compra permitida '));
        
    } else {
        console.log(chalk.bgYellow.black('  ALGO RARO: Nadie pudo comprar '));
    }
})();