#!/bin/sh
set -eu

: "${CONTROLPLANE_API_BASE_URL:=http://localhost:5280/}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{
  "ControlPlaneApi": {
    "BaseUrl": "${CONTROLPLANE_API_BASE_URL}"
  }
}
EOF
