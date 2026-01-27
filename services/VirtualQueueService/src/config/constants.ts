export const QUEUE_CONFIG = {
    TOTAL_TICKETS_KEY: 'queue:total_tickets',
    LAST_SERVED_KEY: 'queue:last_served',
    WORKER_LOCK_KEY: 'queue:worker_lock', 

    RATE_PER_SECOND: 5,       
    BATCH_SIZE: 5,            
    WORKER_INTERVAL_MS: 1000, 
    TICKET_TTL_SECONDS: 3600 
} as const;