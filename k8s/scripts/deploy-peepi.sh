#!/usr/bin/env bash

set -euo pipefail

KUBE_CONTEXT="${KUBE_CONTEXT:-peepi}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
MANIFESTS="$REPOSITORY_ROOT/k8s/manifests"
ENV_FILE="${TRANSFERLAB_ENV_FILE:-$REPOSITORY_ROOT/.env}"
SQL_HOST="${TRANSFERLAB_SQL_HOST:-}"

if [[ -z "$SQL_HOST" ]]; then
    SQL_HOST="$(ipconfig getifaddr en0 2>/dev/null || true)"
fi

if [[ -z "$SQL_HOST" ]]; then
    echo "Set TRANSFERLAB_SQL_HOST to the Mac address reachable from the Pi." >&2
    exit 1
fi

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    if [[ ! -f "$ENV_FILE" ]]; then
        echo "Could not find $ENV_FILE. Set MSSQL_SA_PASSWORD explicitly." >&2
        exit 1
    fi

    MSSQL_SA_PASSWORD="$(awk -F= '$1 == "MSSQL_SA_PASSWORD" { sub(/^[^=]*=/, ""); print; exit }' "$ENV_FILE")"
fi

if [[ -z "$MSSQL_SA_PASSWORD" || "$MSSQL_SA_PASSWORD" == CHANGE_ME_* ]]; then
    echo "Set a real MSSQL_SA_PASSWORD in $ENV_FILE." >&2
    exit 1
fi

escaped_password="${MSSQL_SA_PASSWORD//\"/\"\"}"
connection_string="Server=${SQL_HOST},1433;Database=TransferLab;User Id=sa;Password=\"${escaped_password}\";Encrypt=True;TrustServerCertificate=True"
KUBECTL=(kubectl --context "$KUBE_CONTEXT")
existing_deployments=()

"${KUBECTL[@]}" get nodes >/dev/null
"${KUBECTL[@]}" apply -f "$MANIFESTS/namespace.yaml"

"${KUBECTL[@]}" --namespace transferlab create secret generic transferlab-secrets \
    --from-literal="ConnectionStrings__TransferLab=$connection_string" \
    --dry-run=client \
    --output=yaml |
    "${KUBECTL[@]}" apply -f -

"${KUBECTL[@]}" apply -f "$MANIFESTS/configmap.yaml"
"${KUBECTL[@]}" --namespace transferlab delete job transferlab-migrations \
    --ignore-not-found --wait=true
"${KUBECTL[@]}" apply -f "$MANIFESTS/migrations-job.yaml"

if ! "${KUBECTL[@]}" --namespace transferlab wait \
    --for=condition=complete job/transferlab-migrations --timeout=5m; then
    "${KUBECTL[@]}" --namespace transferlab logs job/transferlab-migrations --all-containers
    exit 1
fi

"${KUBECTL[@]}" --namespace transferlab logs job/transferlab-migrations --all-containers

for deployment in transferlab-api transferlab-worker transferlab-web; do
    if "${KUBECTL[@]}" --namespace transferlab get deployment "$deployment" >/dev/null 2>&1; then
        existing_deployments+=("deployment/$deployment")
    fi
done

"${KUBECTL[@]}" apply -f "$MANIFESTS/api.yaml"
"${KUBECTL[@]}" apply -f "$MANIFESTS/worker.yaml"
"${KUBECTL[@]}" apply -f "$MANIFESTS/web.yaml"

if (( ${#existing_deployments[@]} > 0 )); then
    "${KUBECTL[@]}" --namespace transferlab rollout restart "${existing_deployments[@]}"
fi

"${KUBECTL[@]}" --namespace transferlab rollout status deployment/transferlab-api --timeout=3m
"${KUBECTL[@]}" --namespace transferlab rollout status deployment/transferlab-worker --timeout=3m
"${KUBECTL[@]}" --namespace transferlab rollout status deployment/transferlab-web --timeout=3m

"${KUBECTL[@]}" apply -f "$MANIFESTS/headlamp.yaml"
"${KUBECTL[@]}" --namespace headlamp rollout status deployment/headlamp --timeout=5m

"${KUBECTL[@]}" --namespace transferlab get pods,services,ingress
echo "TransferLab is available at http://192.168.1.179"
echo "Headlamp is available at http://192.168.1.179:30446"
echo "Generate its login token with ./k8s/scripts/headlamp-token.sh"
