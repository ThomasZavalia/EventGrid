
import { fileURLToPath } from 'url';
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
import * as grpc from '@grpc/grpc-js';
import * as protoLoader from '@grpc/proto-loader';
import path from 'path';
import logger from '../config/logger.js';



const PROTO_PATH = process.env.PROTO_PATH || path.resolve(__dirname, '../../../../protos/booking.proto');


const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
});


const bookingProto = grpc.loadPackageDefinition(packageDefinition).booking as any;


const client = new bookingProto.BookingGrpc(
    process.env.GRPC_TARGET || 'localhost:5001',
    grpc.credentials.createInsecure()
);


export const getQueueToken = (userId: string): Promise<string> => {
    return new Promise((resolve, reject) => {
        const deadline = new Date();
        deadline.setSeconds(deadline.getSeconds() + 5);
        client.GetQueueToken({ userId },{deadline}, (err: any, response: any) => {
           if (err) {
                  
                    if (err.code === grpc.status.DEADLINE_EXCEEDED) {
                        logger.error('❌ Timeout: .NET tardó más de 5s en responder');
                    } else {
                        logger.error('❌ Error gRPC llamando a .NET:', err);
                    }
                    return reject(err);
                }
            
            if (!response.success) {
                logger.warn('⚠️ .NET rechazó generar el token:', response.message);
                return reject(new Error(response.message));
            }

            resolve(response.token);
        });
    });
};

export default client;