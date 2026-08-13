#!/usr/bin/env bash
#  EventGrid — dev-setup.sh
#  Levanta todos los servicios y deja el entorno listo para
#  desarrollar o testear.
#  Uso: ./scripts/dev-setup.sh


set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

GATEWAY_URL="http://127.0.0.1:8090"
MAX_WAIT=120

echo -e "${CYAN}"
echo "  EventGrid — Dev Setup"
echo -e "${NC}"

# 1. Verificar Docker
echo -e "${YELLOW}[1/4] Verificando Docker...${NC}"
if ! docker info > /dev/null 2>&1; then
  echo -e "${RED} Docker no está corriendo. Inicialo primero y volvé a correr el script.${NC}"
  exit 1
fi
echo -e "${GREEN} Docker activo${NC}"

# 2. Levantar contenedores
echo -e "\n${YELLOW}[2/4] Levantando contenedores (docker compose up)...${NC}"
docker compose up -d --build
echo -e "${GREEN} Contenedores iniciados${NC}"

# 3. Esperar Gateway
echo -e "\n${YELLOW}[3/4] Esperando que el API Gateway responda en ${GATEWAY_URL}...${NC}"
elapsed=0
until curl -s -X POST "${GATEWAY_URL}/api/queue/join" \
      -H "Content-Type: application/json" \
      -d '{}' \
      -o /dev/null 2>/dev/null; do
  if [ "$elapsed" -ge "$MAX_WAIT" ]; then
    echo -e "\n${RED} Timeout: el Gateway no respondió en ${MAX_WAIT}s. Revisá los logs con 'docker compose logs'.${NC}"
    exit 1
  fi
  printf "."
  sleep 3
  elapsed=$((elapsed + 3))
done
echo -e "\n${GREEN}✓ Gateway listo (tardó ${elapsed}s)${NC}"

# 4. Seed de datos
echo -e "\n${YELLOW}[4/4] Ejecutando seed de datos...${NC}"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${GATEWAY_URL}/api/admin/seed" 2>/dev/null || echo "000")

if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
  echo -e "${GREEN} Seed completado exitosamente (HTTP ${HTTP_CODE})${NC}"
elif [ "$HTTP_CODE" = "409" ]; then
  echo -e "${YELLOW} Seed ya fue ejecutado anteriormente (HTTP 409) — se saltea${NC}"
else
  echo -e "${YELLOW} Seed respondió HTTP ${HTTP_CODE} — revisá si los datos ya existen${NC}"
fi

# Resumen
echo -e "\n${GREEN}"
echo "        Entorno listo para usar"
echo "Gateway: http://127.0.0.1:8090 "
echo "RabbitMQ: http://127.0.0.1:15672 "
echo "Jaeger: http://127.0.0.1:16686 "
echo -e "${NC}"
