#!/usr/bin/env bash
# Arranca la API en modo desarrollo en el puerto 5080.
set -euo pipefail
cd "$(dirname "$0")/.."
if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet no está instalado. Instala .NET 8 SDK desde https://dot.net" >&2
  exit 1
fi
dotnet run --project src/CourierMax.Api --launch-profile http
