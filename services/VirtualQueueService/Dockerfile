
FROM node:20-alpine AS builder
WORKDIR /app


COPY services/VirtualQueueService/package*.json ./
RUN npm ci


COPY services/VirtualQueueService/ ./

COPY protos/ ../../protos/


RUN npm run build


FROM node:20-alpine
WORKDIR /app


COPY --from=builder /app/package*.json ./
RUN npm ci --only=production
COPY --from=builder /app/dist ./dist


COPY protos/ /protos/

EXPOSE 3000

CMD ["node", "dist/index.js"]