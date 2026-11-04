#!/usr/bin/env bash

set -euo pipefail

SSH_TARGET="${SSH_TARGET:-peepi-gemma}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$REPOSITORY_ROOT"

docker build --platform linux/arm64 \
    --tag transferlab-api:peepi \
    --file src/TransferLab.Api/Dockerfile .
docker build --platform linux/arm64 \
    --tag transferlab-worker:peepi \
    --file src/TransferLab.Worker/Dockerfile .
docker build --platform linux/arm64 \
    --tag transferlab-migrations:peepi \
    --file src/TransferLab.Migrations/Dockerfile .
docker build --platform linux/arm64 \
    --tag transferlab-web:peepi \
    --file src/TransferLab.Web/Dockerfile .

docker save \
    transferlab-api:peepi \
    transferlab-worker:peepi \
    transferlab-migrations:peepi \
    transferlab-web:peepi |
    ssh -o BatchMode=yes -o StrictHostKeyChecking=yes "$SSH_TARGET" \
        'sudo k3s ctr images import -'

ssh -o BatchMode=yes -o StrictHostKeyChecking=yes "$SSH_TARGET" \
    "sudo k3s ctr images list -q | grep '^docker.io/library/transferlab-' | sort"
