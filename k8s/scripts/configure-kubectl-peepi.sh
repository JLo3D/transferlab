#!/usr/bin/env bash

set -euo pipefail

SSH_TARGET="${SSH_TARGET:-peepi-gemma}"
PEEPI_ADDRESS="${PEEPI_ADDRESS:-192.168.1.179}"
KUBECONFIG_TARGET="${KUBECONFIG:-$HOME/.kube/config}"
RAW_CONFIG="$(mktemp /tmp/peepi-kubeconfig.raw.XXXXXX)"
NAMED_CONFIG="$(mktemp /tmp/peepi-kubeconfig.named.XXXXXX)"
MERGED_CONFIG="$(mktemp /tmp/peepi-kubeconfig.merged.XXXXXX)"

cleanup() {
    rm -f "$RAW_CONFIG" "$NAMED_CONFIG" "$MERGED_CONFIG"
}
trap cleanup EXIT

ssh -o BatchMode=yes -o StrictHostKeyChecking=yes "$SSH_TARGET" \
    'sudo cat /etc/rancher/k3s/k3s.yaml' >"$RAW_CONFIG"

sed \
    -e "s#server: https://127.0.0.1:6443#server: https://${PEEPI_ADDRESS}:6443#" \
    -e 's/current-context: default/current-context: peepi/' \
    -e 's/cluster: default/cluster: peepi/' \
    -e 's/user: default/user: peepi/' \
    -e 's/name: default/name: peepi/g' \
    "$RAW_CONFIG" >"$NAMED_CONFIG"

chmod 600 "$RAW_CONFIG" "$NAMED_CONFIG" "$MERGED_CONFIG"
install -d -m 700 "$(dirname "$KUBECONFIG_TARGET")"

if [[ -s "$KUBECONFIG_TARGET" ]]; then
    if [[ ! -e "${KUBECONFIG_TARGET}.transferlab-backup" ]]; then
        cp -p "$KUBECONFIG_TARGET" "${KUBECONFIG_TARGET}.transferlab-backup"
    fi
    KUBECONFIG="${KUBECONFIG_TARGET}:${NAMED_CONFIG}" \
        kubectl config view --flatten >"$MERGED_CONFIG"
    install -m 600 "$MERGED_CONFIG" "$KUBECONFIG_TARGET"
else
    install -m 600 "$NAMED_CONFIG" "$KUBECONFIG_TARGET"
fi

kubectl config use-context peepi >/dev/null
kubectl get nodes -o wide
