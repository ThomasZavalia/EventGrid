import redisClient from '../config/redis.js';

export class DistributedLock {
    private key: string;
    private ttl: number; 
    private lockValue: string;

    constructor(key: string, ttl: number = 2000) {
        this.key = `lock:${key}`;
        this.ttl = ttl;
        this.lockValue = `${Date.now()}-${Math.random()}`; 
    }

    
    async acquire(): Promise<boolean> {
        try {
           
            const result = await redisClient.set(
                this.key, 
                this.lockValue, 
                {
                    NX: true,        
                    PX: this.ttl     
                }
            );
            
            return result === 'OK';
        } catch (error) {
            console.error('Error acquiring lock:', error);
            return false;
        }
    }

   
    async release(): Promise<boolean> {
        try {
       
            const script = `
                if redis.call("get", KEYS[1]) == ARGV[1] then
                    return redis.call("del", KEYS[1])
                else
                    return 0
                end
            `;
            
            const result = await redisClient.eval(script, {
                keys: [this.key],
                arguments: [this.lockValue]
            });
            
            return result === 1;
        } catch (error) {
            console.error('Error releasing lock:', error);
            return false;
        }
    }
}