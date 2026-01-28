import type { Request, Response } from 'express';
import redisClient from '../config/redis.js';
import logger from '../config/logger.js';
import { QUEUE_CONFIG } from '../config/constants.js';
import { retryWithBackoff } from '../utils/retry.js';
import { getQueueToken } from '../grpc/client.js';

export const joinQueue = async (req: Request, res: Response): Promise<void> => {
    try {
        const { userId } = req.body;
        
        if (!userId) {
            res.status(400).json({ error: 'UserId requerido' });
            return;
        }

        const userTicketKey = `queue:user:${userId}`;
        const existingTicket = await redisClient.get(userTicketKey);

        let myTicketNumber: number;

        if (existingTicket) {
            myTicketNumber = parseInt(existingTicket);
            logger.info(` Usuario ${userId} recuperó ticket #${myTicketNumber}`);
        } else {
           
            myTicketNumber = await redisClient.incr(QUEUE_CONFIG.TOTAL_TICKETS_KEY);
          
            await redisClient.setEx(
                userTicketKey, 
                QUEUE_CONFIG.TICKET_TTL_SECONDS, 
                myTicketNumber.toString()
            );
            logger.info(` Usuario ${userId} obtuvo ticket #${myTicketNumber}`);
        }

    
        const lastServedStr = await redisClient.get(QUEUE_CONFIG.LAST_SERVED_KEY);
        const lastServed = lastServedStr ? parseInt(lastServedStr) : 0;
        
        const position = myTicketNumber - lastServed;
        let accessToken = null;
       const canEnter = position <= QUEUE_CONFIG.BATCH_SIZE;
        

        if (canEnter) {
            
            const cachedTokenKey = `queue:token:${userId}`;
            const cachedToken = await redisClient.get(cachedTokenKey);

            if (cachedToken) {
                logger.info(` Token recuperado de cache para ${userId}`);
                accessToken = cachedToken;
            } else {
               
                try {
                    logger.info(` Solicitando token a .NET para usuario ${userId}...`);
                    
                    accessToken = await retryWithBackoff(
                        () => getQueueToken(userId),
                        3,  
                        200 
                    );

                    
                    await redisClient.setEx(cachedTokenKey, 900, accessToken);
                    logger.info(` Token recibido de .NET y cacheado.`);

                } catch (error) {
                    logger.error(' Falló la obtención del token tras reintentos:', error);
                
                    res.status(502).json({ error: 'El servicio de reservas no responde. Intente nuevamente.' });
                    return;
                }
            }
        }

        res.json({
            ticketNumber: myTicketNumber,
            currentServing: lastServed,
            position: position > 0 ? position : 0,
            status: canEnter ? 'PROCESSED' : 'WAITING',
            accessToken: accessToken 
        });

    } catch (error) {
        logger.error('Error al unirse a la cola:', error);
        res.status(500).json({ error: 'Error interno' });
    }
};


export const getQueueStatus = async (req: Request, res: Response): Promise<void> => {
    try {
       
        const { ticketNumber } = req.query;
        
        if (!ticketNumber) {
             res.status(400).json({ error: 'TicketNumber requerido' });
             return;
        }

        const myTicket = parseInt(ticketNumber as string);

         if (isNaN(myTicket) || myTicket <= 0) {
            res.status(400).json({ error: 'TicketNumber debe ser un número válido' });
            return;
        }

        const lastServedStr = await redisClient.get(QUEUE_CONFIG.LAST_SERVED_KEY);
        const lastServed = lastServedStr ? parseInt(lastServedStr) : 0;
        const position = myTicket - lastServed;
        const canEnter = position <= QUEUE_CONFIG.BATCH_SIZE;

        res.json({
            ticketNumber: myTicket,
            currentServing: lastServed,
            position: position > 0 ? position : 0,
            status: canEnter ? 'PROCESSED' : 'WAITING'
        });

    } catch (error) {
        console.error('Error getting status:', error);
        res.status(500).json({ error: 'Error interno' });
    }
};