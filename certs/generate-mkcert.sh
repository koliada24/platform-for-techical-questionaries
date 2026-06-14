#!/usr/bin/env bash
# Generates a locally-trusted cert for localhost using mkcert.
# Run `mkcert -install` once before this (adds the local CA to the OS/browser trust store).
set -euo pipefail

cd "$(dirname "$0")"

PFX_PASSWORD="${PFX_PASSWORD:-devpassword}"

if ! command -v mkcert >/dev/null 2>&1; then
  echo "mkcert is not installed. Install it (e.g. 'winget install FiloSottile.mkcert')," >&2
  echo "run 'mkcert -install' once, then re-run this script." >&2
  exit 1
fi

mkcert -cert-file server.crt -key-file server.key localhost 127.0.0.1 ::1

openssl pkcs12 -export \
  -out server.pfx \
  -inkey server.key \
  -in server.crt \
  -passout "pass:${PFX_PASSWORD}"

echo "Done. Files generated in $(pwd):"
ls -1 server.crt server.key server.pfx
