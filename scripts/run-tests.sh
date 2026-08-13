#!/usr/bin/env bash
#  EventGrid — run-tests.sh
#  Ejecuta la suite completa de tests: integración C# + load
#  test de k6. Muestra un resumen final con pass/fail.
#
#  Uso:
#    ./scripts/run-tests.sh               # solo tests C#
#    ./scripts/run-tests.sh <seat-uuid>   # C# + k6 load test


GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

SEAT_ID="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
TESTS_PATH="${ROOT_DIR}/tests/BookingService.IntegrationTests"
K6_SCRIPT="${ROOT_DIR}/tests/stress-test/k6-load-test.js"

PASS=0
FAIL=0

echo -e "${CYAN}"
echo "  EventGrid — Test Runner"
echo -e "${NC}"

DOTNET_CMD=""
if command -v dotnet > /dev/null 2>&1; then
  DOTNET_CMD="dotnet"
elif command -v dotnet.exe > /dev/null 2>&1; then
  DOTNET_CMD="dotnet.exe"
fi

# 1. Tests de integración C#
echo -e "${YELLOW}[1/2] Ejecutando tests de integración C# (xUnit + Testcontainers)...${NC}"
echo -e "      Proyecto: ${TESTS_PATH}\n"

if [ -z "$DOTNET_CMD" ]; then
  echo -e "${YELLOW}  dotnet no encontrado en PATH. Instalalo desde https://dot.net${NC}"
  FAIL=$((FAIL + 1))
else
 
  TESTS_PATH_CMD="${TESTS_PATH}"
  if [ "$DOTNET_CMD" = "dotnet.exe" ] && command -v wslpath > /dev/null 2>&1; then
    TESTS_PATH_CMD=$(wslpath -w "${TESTS_PATH}")
  fi

  if $DOTNET_CMD test "${TESTS_PATH_CMD}" --verbosity minimal 2>&1; then
    echo -e "\n${GREEN}✓ Tests de integración: PASARON${NC}"
    PASS=$((PASS + 1))
  else
    echo -e "\n${RED}✗ Tests de integración: FALLARON${NC}"
    FAIL=$((FAIL + 1))
  fi
fi

# 2. Load test k6
echo -e "\n${YELLOW}[2/2] Load test con k6 (50 VUs concurrentes)...${NC}"

if [ -z "$SEAT_ID" ]; then
  echo -e "${YELLOW}  k6 salteado: no se pasó un SEAT_ID.${NC}"
  echo -e "   Uso: ${BOLD}./scripts/run-tests.sh <seat-uuid>${NC}"
  echo -e "   Obtené un ID con: ${BOLD}curl -X POST http://127.0.0.1:8090/api/admin/seed${NC}"
elif ! command -v k6 > /dev/null 2>&1; then
  echo -e "${YELLOW}  k6 no encontrado. Instalalo con:${NC}"
  echo -e "   ${BOLD}winget install k6${NC}  (Windows)"
  echo -e "   ${BOLD}brew install k6${NC}    (macOS)"
else
  echo -e "   SEAT_ID: ${SEAT_ID}\n"
  if k6 run --env SEAT_ID="${SEAT_ID}" "${K6_SCRIPT}"; then
    echo -e "\n${GREEN}✓ Load test k6: PASÓ (0 race conditions, 0 errores 500)${NC}"
    PASS=$((PASS + 1))
  else
    echo -e "\n${RED}✗ Load test k6: FALLO  revisá los thresholds en la salida de arriba${NC}"
    FAIL=$((FAIL + 1))
  fi
fi

# Resumen final
echo ""
printf "${GREEN}%-6s${NC} Pasaron   ${RED}%-6s${NC} Fallaron" "✓ ${PASS}" "✗ ${FAIL}"
echo ""

[ "$FAIL" -eq 0 ] && exit 0 || exit 1
