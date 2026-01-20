import { Router } from 'express';
import { joinQueue, getQueueStatus } from '../controllers/queueController.js';

const router = Router();

router.post('/join', joinQueue);
router.get('/status', getQueueStatus);

export default router;