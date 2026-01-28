import logger from '../config/logger.js'; 



export async function retryWithBackoff<T>(
    fn: () => Promise<T>,
    maxRetries: number = 3,
    initialDelay: number = 100
): Promise<T> {
    let lastError: Error | undefined;
    
    for (let attempt = 0; attempt < maxRetries; attempt++) {
        try {
            return await fn();
        } catch (error) {
            lastError = error as Error;
            
          
            if (attempt === maxRetries - 1) break;

            
            const delay = initialDelay * Math.pow(2, attempt); 
            logger.warn(` Intento fallido (${attempt + 1}/${maxRetries}). Reintentando en ${delay}ms... Error: ${lastError.message}`);
            
            await new Promise(resolve => setTimeout(resolve, delay));
        }
    }
    
    throw lastError;
}