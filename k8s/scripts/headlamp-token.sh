#!/usr/bin/env bash

set -euo pipefail

KUBE_CONTEXT="${KUBE_CONTEXT:-peepi}"
TOKEN_DURATION="${TOKEN_DURATION:-8h}"

echo "Paste this short-lived token into Headlamp:"
kubectl --context "$KUBE_CONTEXT" \
    --namespace headlamp \
    create token headlamp-admin \
    --duration "$TOKEN_DURATION"
