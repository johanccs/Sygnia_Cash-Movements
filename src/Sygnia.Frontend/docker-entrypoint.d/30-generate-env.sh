#!/bin/sh
# Regenerates env.js from the GRPC_URL env var before nginx starts, so one built image can
# point at either the native Presentation (:5058, run-local.ps1) or the `presentation`
# container (:8080, run-dockerhub.ps1) depending on what docker-compose passes in.
set -eu

GRPC_URL="${GRPC_URL:-http://localhost:5058}"

cat > /usr/share/nginx/html/env.js <<EOF
window.__env = {
  grpcUrl: '${GRPC_URL}',
};
EOF
