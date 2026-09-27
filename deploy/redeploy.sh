#!/usr/bin/env bash
# Update an existing LandWealth server.
# Pulls master, publishes the API, builds the web app, applies EF migrations,
# and restarts landwealth-api. Does not change MySQL users, /etc/landwealth.env,
# nginx, or the TLS certificate.
#   sudo bash /opt/landwealth/src/deploy/redeploy.sh
set -euo pipefail

REPO_DIR="/opt/landwealth/src"
API_DIR="/opt/landwealth/api"
WEB_DIR="${REPO_DIR}/src/landwealth-web"
WEB_ROOT="/var/www/landwealth"
DOCUMENT_ROOT="/var/lib/landwealth/documents"
ENV_FILE="/etc/landwealth.env"

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run this script as root."
  exit 1
fi

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing ${ENV_FILE}. Run deploy/setup.sh on a new server."
  exit 1
fi

if [[ ! -d "${REPO_DIR}/.git" ]]; then
  echo "Missing git checkout at ${REPO_DIR}."
  exit 1
fi

git -C "${REPO_DIR}" pull --ff-only origin master

systemctl stop landwealth-api || true
rm -rf "${API_DIR}"
dotnet publish "${REPO_DIR}/src/LandWealth.Api/LandWealth.Api.csproj" -c Release -o "${API_DIR}"
test -f "${API_DIR}/Microsoft.OpenApi.dll"

cd "${WEB_DIR}"
npm ci
npm run build
mkdir -p "${WEB_ROOT}" "${DOCUMENT_ROOT}"
rm -rf "${WEB_ROOT:?}/"*
cp -a dist/. "${WEB_ROOT}/"

cd "${REPO_DIR}"
set -a
# shellcheck disable=SC1090
source "${ENV_FILE}"
set +a
dotnet tool uninstall --global dotnet-ef || true
dotnet tool install --global dotnet-ef --version 9.0.20 --allow-roll-forward
export PATH="${PATH}:${HOME}/.dotnet/tools"
dotnet ef database update --project src/LandWealth.Infrastructure --startup-project src/LandWealth.Api

chown -R www-data:www-data "${API_DIR}" "${WEB_ROOT}" "${DOCUMENT_ROOT}"

systemctl daemon-reload
systemctl reset-failed landwealth-api || true
systemctl enable landwealth-api
systemctl restart landwealth-api
sleep 3
curl -fsS http://127.0.0.1:5000/api/health
echo

nginx -t
systemctl reload nginx

echo "LandWealth redeploy finished."
