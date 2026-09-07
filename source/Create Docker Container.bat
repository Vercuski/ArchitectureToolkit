docker compose -f docker-compose.yml -f docker-compose.with-local-tls.yml up

REM docker compose exec caddy cat /data/caddy/pki/authorities/local/root.crt > architecturetoolkit-local-ca.crt