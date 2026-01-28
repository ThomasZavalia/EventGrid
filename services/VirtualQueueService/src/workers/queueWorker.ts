import redisClient from '../config/redis.js';
import { DistributedLock } from '../utils/distributedLock.js';
import { QUEUE_CONFIG } from '../config/constants.js';

let workerInterval: NodeJS.Timeout | null = null;

export const startQueueWorker = () => {
    console.log(' Queue Worker iniciado...');

    workerInterval = setInterval(async () => {
        const lock = new DistributedLock('queue_worker', 2000);
        
        try {
        
            const acquired = await lock.acquire();
            
            if (!acquired) {
                
                return;
            }
            
      
            const totalTicketsStr = await redisClient.get(QUEUE_CONFIG.TOTAL_TICKETS_KEY);
            const lastServedStr = await redisClient.get(QUEUE_CONFIG.LAST_SERVED_KEY);

            if (!totalTicketsStr) return;

            const totalTickets = parseInt(totalTicketsStr);
            let lastServed = lastServedStr ? parseInt(lastServedStr) : 0;

            if (lastServed >= totalTickets) return;

            let nextBatch = lastServed + QUEUE_CONFIG.RATE_PER_SECOND;
            if (nextBatch > totalTickets) {
                nextBatch = totalTickets;
            }

            await redisClient.set(QUEUE_CONFIG.LAST_SERVED_KEY, nextBatch.toString());
            
            console.log(` [Worker] Atendiendo hasta ticket #${nextBatch}. (Faltan: ${totalTickets - nextBatch})`);

        } catch (error) {
            console.error(' Error en Queue Worker:', error);
        } finally {
            
            await lock.release();
        }
    }, QUEUE_CONFIG.WORKER_INTERVAL_MS);
};

export const stopQueueWorker = () => {
    if (workerInterval) {
        clearInterval(workerInterval);
        workerInterval = null;
        console.log(' Queue Worker detenido');
    }
};