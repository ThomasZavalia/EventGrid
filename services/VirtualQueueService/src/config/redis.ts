import { createClient } from 'redis';
import dotenv from 'dotenv';

dotenv.config();

const client = createClient({
    url: process.env.REDIS_URL || 'redis://localhost:6379'
});

client.on('error', (err) => console.error(' Redis Client Error:', err));
client.on('connect', () => console.log(' Conectando a Redis...'));
client.on('ready', () => console.log(' Redis listo para usar'));

export const connectRedis = async () => {
    try {
        await client.connect();
    } catch (error) {
        console.error(' No se pudo conectar a Redis:', error);
       
        process.exit(1); 
    }
};

export default client;




