import { startTracing } from './config/tracing.js';
startTracing();
import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { connectRedis } from './config/redis.js';
import redisClient from './config/redis.js';
import queueRoutes from './routes/queueRoutes.js';
import { startQueueWorker, stopQueueWorker } from './workers/queueWorker.js';
import logger from './config/logger.js';
import { getQueueToken } from './grpc/client.js';


dotenv.config();

const app = express();
const port = process.env.PORT || 3000;

app.use(express.json());
app.use(cors());
app.use('/api/queue', queueRoutes);


app.get('/health', async (req, res) => {
    try {
       
        await redisClient.ping();
        
      
        let grpcStatus = 'unknown';
        try {
           
            await getQueueToken('HEALTH_CHECK_PROBE'); 
            grpcStatus = 'connected';
        } catch (error) {
           
            const err = error as any;
            if (err.code === 14) { 
                 grpcStatus = 'disconnected';
            } else {
             
                 grpcStatus = 'connected';
            }
        }

       
        const isCriticalFailure = false; 

        res.status(isCriticalFailure ? 503 : 200).json({ 
            status: grpcStatus === 'connected' ? 'OK' : 'DEGRADED', 
            service: 'VirtualQueueService',
            redis: 'connected',
            grpc: grpcStatus
        });

    } catch (error) {
        res.status(503).json({ 
            status: 'ERROR', 
            service: 'VirtualQueueService',
            redis: 'disconnected',
            error: error instanceof Error ? error.message : 'Unknown error'
        });
    }
});

const startServer = async () => {
    try {
        await connectRedis();
        startQueueWorker();
        
        const server = app.listen(port, () => {
            console.log(`🚀 Virtual Queue Service corriendo en http://localhost:${port}`);
        });

      
        const gracefulShutdown = async () => {
            logger.info('🛑 Cerrando servidor...');
    
            stopQueueWorker();
            
            server.close(async () => {
                console.log(' Servidor HTTP cerrado');
                try {
                    await redisClient.quit();
                    console.log(' Conexión a Redis cerrada');
                    process.exit(0);
                } catch (err) {
                    console.error('Error al cerrar Redis:', err);
                    process.exit(1);
                }
            });
        };

      
        process.on('SIGTERM', gracefulShutdown);
        process.on('SIGINT', gracefulShutdown);

    } catch (error) {
        console.error('Error fatal al iniciar:', error);
        process.exit(1);
    }
};

startServer();