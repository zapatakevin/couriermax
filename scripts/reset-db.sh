#!/usr/bin/env bash
# Borra la base de datos SQLite local.
set -euo pipefail
cd "$(dirname "$0")/.."
find . -maxdepth 4 -name "*.db" -not -path "*/bin/*" -not -path "*/obj/*" -delete || true
echo "DB local eliminada."
