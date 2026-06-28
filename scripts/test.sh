#!/usr/bin/env bash
# Ejecuta la suite de tests con cobertura.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet test --configuration Release --collect:"XPlat Code Coverage"
