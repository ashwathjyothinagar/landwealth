#!/usr/bin/env bash

# Update an existing LandWealth server.
# Pulls master, publishes the API, builds the web app, applies EF migrations,
# and restarts landwealth-api.
# Does not change MySQL users, /etc/landwealth.env, nginx, or the TLS certificate.

set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
    echo "Run this script as root."
    exit 1
fi

if [[ ! -f "/etc/landwealth.env" ]]; then
    echo "Missing /etc/landwealth.env. Run deploy/setup.sh on a new server."
    exit 1
fi

if [[ ! -d "/opt/landwealth/src/.git" ]]; then
    echo "Missing git checkout at /opt/landwealth/src."
    exit 1
fi

echo "Pulling latest code..."
git -C "/opt/landwealth/src" pull --ff-only origin master

echo "Stopping LandWealth API..."
systemctl stop landwealth-api || true

echo "Publishing API..."
rm -rf "/opt/landwealth/api"

dotnet publish \
    "/opt/landwealth/src/src/LandWealth.Api/LandWealth.Api.csproj" \
    -c Release \
    -o "/opt/landwealth/api"

test -f "/opt/landwealth/api/Microsoft.OpenApi.dll"

echo "Building web application..."
cd "/opt/landwealth/src/src/landwealth-web"

npm ci

npm run build

echo "Updating web files..."
mkdir -p "/var/www/landwealth" "/var/lib/landwealth/documents"

rm -rf "/var/www/landwealth"/*

cp -a dist/. "/var/www/landwealth/"

echo "Loading environment..."
cd "/opt/landwealth/src"

set -a
source "/etc/landwealth.env"
set +a

echo "Installing dotnet-ef..."
dotnet tool uninstall --global dotnet-ef || true

dotnet tool install \
    --global dotnet-ef \
    --version 9.0.20 \
    --allow-roll-forward

export PATH="${PATH}:/root/.dotnet/tools"

echo "Applying database migrations..."

dotnet ef database update \
    --project "src/LandWealth.Infrastructure" \
    --startup-project "src/LandWealth.Api"

echo "Setting permissions..."

chown -R www-data:www-data \
    "/opt/landwealth/api" \
    "/var/www/landwealth" \
    "/var/lib/landwealth/documents"

echo "Starting LandWealth API..."

systemctl daemon-reload

systemctl reset-failed landwealth-api || true

systemctl enable landwealth-api

systemctl restart landwealth-api

echo "Checking API health..."

sleep 3

curl -fsS http://127.0.0.1:5000/api/health

echo

echo "Checking nginx..."

nginx -t

systemctl reload nginx

echo "LandWealth redeploy finished."