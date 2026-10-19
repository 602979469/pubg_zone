#!/bin/bash
# Lightweight CI for the public server: poll GitHub, sync the tarball, restart.
set -euo pipefail

BASE_DIR="/opt/pubg-zone-ci"
DEPLOY_DIR="/opt/pubg-zone"
REPO="602979469/pubg_zone"
BRANCH="main"
SERVICE="pubg-zone"
STATE_FILE="${BASE_DIR}/state"
LOG_FILE="${BASE_DIR}/pipeline.log"
LOCK_FILE="${BASE_DIR}/pipeline.lock"

log() {
  echo "[$(date '+%F %T')] $*" >> "${LOG_FILE}"
}

mkdir -p "${BASE_DIR}"
exec 9>"${LOCK_FILE}"
flock -n 9 || exit 0

LATEST=$(curl -fsS --max-time 20 \
  -H 'Accept: application/vnd.github+json' \
  "https://api.github.com/repos/${REPO}/commits/${BRANCH}" \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["sha"])')

LAST=$(cat "${STATE_FILE}" 2>/dev/null || true)
if [ "${LATEST}" = "${LAST}" ]; then
  exit 0
fi

SHORT=${LATEST:0:7}
TMP=$(mktemp -d)
trap 'rm -rf "${TMP}"' EXIT

curl -fsSL --max-time 60 \
  "https://codeload.github.com/${REPO}/tar.gz/${LATEST}" \
  -o "${TMP}/src.tar.gz"
tar xzf "${TMP}/src.tar.gz" -C "${TMP}"
SRC=$(find "${TMP}" -maxdepth 1 -mindepth 1 -type d | head -1)

if [ -z "${SRC}" ]; then
  log "source archive has no directory"
  exit 1
fi

# Keep the runtime cache; the app refreshes it itself.
rsync -a --delete \
  --exclude 'data/rotation.json' \
  --exclude '__pycache__' \
  "${SRC}/" "${DEPLOY_DIR}/"
chown -R pubg:pubg "${DEPLOY_DIR}"

systemctl restart "${SERVICE}"
for _ in $(seq 1 20); do
  if curl -fsS --max-time 2 http://127.0.0.1:8765/health >/dev/null; then
    echo "${LATEST}" > "${STATE_FILE}"
    log "deployed ${SHORT}"
    exit 0
  fi
  sleep 1
done

log "deploy ${SHORT} failed: health check timeout"
exit 1
