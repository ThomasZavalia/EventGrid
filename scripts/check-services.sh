#!/usr/bin/env bash
# EventGrid — check-services.sh
#  Verifica el estado de todos los servicios del stack y
#  muestra un resumen en la terminal.
#  Uso: ./scripts/check-services.sh


GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

ALL_UP=true

# Helper: imprime resultado de un check
print_status() {
  local name="$1"
  local status="$2"   
  local detail="$3"

  if [ "$status" = "ok" ]; then
    echo -e "  ${GREEN}✓${NC}  ${BOLD}$(printf '%-20s' "$name")${NC} ${GREEN}UP${NC}    ${detail}"
  else
    echo -e "  ${RED}✗${NC}  ${BOLD}$(printf '%-20s' "$name")${NC} ${RED}DOWN${NC}  ${detail}"
    ALL_UP=false
  fi
}

echo -e "${CYAN}"
echo "EventGrid — Health Check"
echo -e "${NC}"

GW_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 4 \
  -X POST "http://127.0.0.1:8090/api/queue/join" \
  -H "Content-Type: application/json" \
  -d '{}' 2>/dev/null || echo "000")


if [ "$GW_CODE" = "400" ] || [ "$GW_CODE" = "200" ]; then
  print_status "API Gateway" "ok" "puerto 8090"
else
  print_status "API Gateway" "fail" "puerto 8090 (HTTP ${GW_CODE})"
fi

# Redis
REDIS_RESP=$(docker exec eventgrid-redis redis-cli ping 2>/dev/null || echo "ERROR")
if [ "$REDIS_RESP" = "PONG" ]; then
  print_status "Redis" "ok" "puerto 6379"
else
  print_status "Redis" "fail" "puerto 6379"
fi

# RabbitMQ
RABBIT_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 4 \
  -u "guest:guest" \
  "http://127.0.0.1:15672/api/overview" 2>/dev/null || echo "000")

if [ "$RABBIT_CODE" = "200" ]; then
  print_status "RabbitMQ" "ok" "puerto 15672"
else
  print_status "RabbitMQ" "fail" "puerto 15672 (HTTP ${RABBIT_CODE})"
fi

# PostgreSQL
docker exec eventgrid-postgres pg_isready -U admin -d eventgrid_booking -q 2>/dev/null
PG_STATUS=$?
if [ "$PG_STATUS" = "0" ]; then
  print_status "PostgreSQL" "ok" "puerto 5434"
else
  print_status "PostgreSQL" "fail" "puerto 5434"
fi

# Jaeger
JAEGER_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 4 \
  "http://127.0.0.1:16686" 2>/dev/null || echo "000")

if [ "$JAEGER_CODE" = "200" ]; then
  print_status "Jaeger (Trazas)" "ok" "puerto 16686"
else
  print_status "Jaeger (Trazas)" "fail" "puerto 16686 (HTTP ${JAEGER_CODE})"
fi

# Resumen
echo ""
if [ "$ALL_UP" = true ]; then
  echo "   Todos los servicios están UP"
  exit 0
else
  echo "   Uno o más servicios están DOWN"
  exit 1
fi
