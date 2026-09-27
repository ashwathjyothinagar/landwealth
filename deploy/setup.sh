#!/usr/bin/env bash
# First-time LandWealth setup on Ubuntu 24.04 with .NET 10.
# Edit the four values below, copy this file to the server, then run:
#   sudo bash setup.sh
# Later updates use deploy/redeploy.sh. This script stops if /etc/landwealth.env
# already exists so it does not replace the live password or JWT secret.
set -euo pipefail

DOMAIN="landwealth.example.com"
MYSQL_PASSWORD="a-long-random-password"
JWT_SECRET="replace-with-at-least-32-random-characters"
CERTBOT_EMAIL="you@example.com"

REPO_URL="https://github.com/ashwathjyothinagar/landwealth.git"
REPO_DIR="/opt/landwealth/src"

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run this script as root."
  exit 1
fi

if [[ -f /etc/landwealth.env ]]; then
  echo "/etc/landwealth.env already exists. Use deploy/redeploy.sh to update this server."
  exit 1
fi

if [[ "${#JWT_SECRET}" -lt 32 ]]; then
  echo "JWT_SECRET must be at least 32 characters."
  exit 1
fi

if [[ "${MYSQL_PASSWORD}" == *"'"* || "${MYSQL_PASSWORD}" == *";"* ]]; then
  echo "MYSQL_PASSWORD cannot contain a single quote or a semicolon."
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive

apt update
apt install -y ca-certificates curl gnupg nginx certbot python3-certbot-nginx git libaio1t64
ln -sf /usr/lib/x86_64-linux-gnu/libaio.so.1t64 /usr/lib/x86_64-linux-gnu/libaio.so.1
apt install -y mysql-server
systemctl enable --now mysql

mysql <<SQL
CREATE DATABASE IF NOT EXISTS landwealth CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'landwealth_user'@'localhost' IDENTIFIED BY '${MYSQL_PASSWORD}';
ALTER USER 'landwealth_user'@'localhost' IDENTIFIED BY '${MYSQL_PASSWORD}';
GRANT ALL PRIVILEGES ON landwealth.* TO 'landwealth_user'@'localhost';
FLUSH PRIVILEGES;
SQL

wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
apt update
apt install -y dotnet-sdk-10.0
dotnet --version

if ! command -v node >/dev/null 2>&1 || ! node -v | grep -q '^v20\.'; then
  curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  apt install -y nodejs
fi

if [[ ! -d "${REPO_DIR}/.git" ]]; then
  git clone "${REPO_URL}" "${REPO_DIR}"
fi

mkdir -p /var/www/landwealth /var/lib/landwealth/documents

cat > /etc/landwealth.env <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=landwealth;User=landwealth_user;Password=${MYSQL_PASSWORD};"
JwtSettings__SecretKey=${JWT_SECRET}
JwtSettings__Issuer=LandWealth.Api
JwtSettings__Audience=LandWealth.Web
StorageSettings__DocumentRoot=/var/lib/landwealth/documents
EOF
chmod 600 /etc/landwealth.env

cat > /etc/systemd/system/landwealth-api.service <<'EOF'
[Unit]
Description=LandWealth API
After=network.target mysql.service

[Service]
WorkingDirectory=/opt/landwealth/api
ExecStart=/usr/bin/dotnet /opt/landwealth/api/LandWealth.Api.dll
EnvironmentFile=/etc/landwealth.env
Restart=always
User=www-data
Group=www-data

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload

cat > /etc/nginx/sites-available/landwealth <<EOF
server {
    listen 80;
    server_name ${DOMAIN};
    root /var/www/landwealth;
    index index.html;

    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location / {
        try_files \$uri \$uri/ /index.html;
    }
}
EOF

ln -sf /etc/nginx/sites-available/landwealth /etc/nginx/sites-enabled/landwealth
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ -f "${SCRIPT_DIR}/redeploy.sh" ]]; then
  bash "${SCRIPT_DIR}/redeploy.sh"
else
  bash "${REPO_DIR}/deploy/redeploy.sh"
fi

certbot --nginx -d "${DOMAIN}" --non-interactive --agree-tos -m "${CERTBOT_EMAIL}"

echo "LandWealth is available at https://${DOMAIN}/register"
