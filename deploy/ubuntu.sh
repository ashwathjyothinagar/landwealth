#!/usr/bin/env bash
# LandWealth on Ubuntu 24.04 with .NET 10.
# Edit the four values below, then run as root: bash ubuntu.sh
set -euo pipefail

DOMAIN="landwealth.example.com"
MYSQL_PASSWORD="a-long-random-password"
JWT_SECRET="replace-with-at-least-32-random-characters"
CERTBOT_EMAIL="you@example.com"

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run this script as root."
  exit 1
fi

if [[ "${#JWT_SECRET}" -lt 32 ]]; then
  echo "JWT_SECRET must be at least 32 characters."
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

if [[ -d /opt/landwealth/src/.git ]]; then
  git -C /opt/landwealth/src pull origin master
else
  git clone https://github.com/ashwathjyothinagar/landwealth.git /opt/landwealth/src
fi

cd /opt/landwealth/src
dotnet publish src/LandWealth.Api/LandWealth.Api.csproj -c Release -o /opt/landwealth/api

cd /opt/landwealth/src/src/landwealth-web
npm ci
npm run build
mkdir -p /var/www/landwealth /var/lib/landwealth/documents
cp -r dist/* /var/www/landwealth/

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

cd /opt/landwealth/src
set -a
# shellcheck disable=SC1091
source /etc/landwealth.env
set +a
dotnet tool uninstall --global dotnet-ef || true
dotnet tool install --global dotnet-ef --version 9.0.20 --allow-roll-forward
export PATH="${PATH}:${HOME}/.dotnet/tools"
dotnet ef database update --project src/LandWealth.Infrastructure --startup-project src/LandWealth.Api

chown -R www-data:www-data /opt/landwealth/api /var/www/landwealth /var/lib/landwealth

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
systemctl enable --now landwealth-api
systemctl restart landwealth-api
curl -fsS http://127.0.0.1:5000/api/health
echo

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

certbot --nginx -d "${DOMAIN}" --non-interactive --agree-tos -m "${CERTBOT_EMAIL}"

echo "LandWealth is available at https://${DOMAIN}/register"
