# ETAPA 1: Build
FROM node:20-alpine AS builder
WORKDIR /app

# Copiar package.json e instalar dependencias
COPY services/VirtualQueueService/package*.json ./
RUN npm ci

# Copiar código fuente y PROTOS
COPY services/VirtualQueueService/ ./
# Copiamos los protos a una carpeta interna para que el build los tenga
COPY protos/ ../../protos/

# Compilar TypeScript
RUN npm run build

# ETAPA 2: Runtime
FROM node:20-alpine
WORKDIR /app

# Copiar dependencias de producción y código compilado
COPY --from=builder /app/package*.json ./
RUN npm ci --only=production
COPY --from=builder /app/dist ./dist

# IMPORTANTE: Copiar los protos a la imagen final manteniendo la ruta relativa que espera tu código
# Tu código busca en: ../../../../protos/booking.proto
# Si estamos en /app/dist/grpc/client.js (aprox), necesitamos subir 4 niveles.
# Vamos a simplificar copiándolos a una ruta fija y usando variable de entorno.
COPY protos/ /protos/

EXPOSE 3000

CMD ["node", "dist/index.js"]