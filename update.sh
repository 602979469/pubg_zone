#!/bin/bash
# Pull the latest code and restart the public service. No arguments.
set -euo pipefail

cd "$(dirname "$0")"

if [ "$(id -u)" -eq 0 ]; then
  git config --global --add safe.directory "$PWD" 2>/dev/null || true
fi

echo "Pulling latest code..."
git pull --ff-only
if [ "$(id -u)" -eq 0 ]; then
  chown -R pubg:pubg "$PWD"
fi

echo "Restarting pubg-zone..."
if [ "$(id -u)" -eq 0 ]; then
  systemctl restart pubg-zone
else
  sudo systemctl restart pubg-zone
fi

for _ in $(seq 1 20); do
  if curl -fsS --max-time 2 http://127.0.0.1:8765/health >/dev/null; then
    echo "OK: $(git rev-parse --short HEAD)"
    exit 0
  fi
  sleep 1
done

echo "ERROR: health check failed" >&2
exit 1
