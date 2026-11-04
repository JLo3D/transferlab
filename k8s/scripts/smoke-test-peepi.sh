#!/usr/bin/env bash

set -euo pipefail

BASE_URL="${TRANSFERLAB_URL:-http://192.168.1.179}"
RUN_ID="$(date +%s)"
IDEMPOTENCY_KEY="peepi-smoke-${RUN_ID}"
RESPONSE_FILE="$(mktemp /tmp/transferlab-smoke.XXXXXX)"

cleanup() {
    rm -f "$RESPONSE_FILE"
}
trap cleanup EXIT

for command in curl jq; do
    if ! command -v "$command" >/dev/null; then
        echo "$command is required." >&2
        exit 1
    fi
done

printf 'Web health: '
curl --fail --silent --show-error "$BASE_URL/health/live"

source_id="$(
    curl --fail --silent --show-error \
        --header 'Content-Type: application/json' \
        --data "{\"name\":\"Peepi Source ${RUN_ID}\",\"openingBalance\":500}" \
        "$BASE_URL/api/accounts/" |
        jq --exit-status --raw-output .id
)"

destination_id="$(
    curl --fail --silent --show-error \
        --header 'Content-Type: application/json' \
        --data "{\"name\":\"Peepi Destination ${RUN_ID}\",\"openingBalance\":100}" \
        "$BASE_URL/api/accounts/" |
        jq --exit-status --raw-output .id
)"

transfer_payload="{\"sourceAccountId\":\"${source_id}\",\"destinationAccountId\":\"${destination_id}\",\"amount\":125}"
create_status="$(
    curl --silent --show-error \
        --output "$RESPONSE_FILE" \
        --write-out '%{http_code}' \
        --header 'Content-Type: application/json' \
        --header "Idempotency-Key: ${IDEMPOTENCY_KEY}" \
        --data "$transfer_payload" \
        "$BASE_URL/api/transfers/"
)"

if [[ "$create_status" != 201 ]]; then
    echo "Expected transfer creation HTTP 201; received $create_status." >&2
    cat "$RESPONSE_FILE" >&2
    exit 1
fi

transfer_id="$(jq --exit-status --raw-output .id "$RESPONSE_FILE")"

for _ in {1..30}; do
    transfer_status="$(
        curl --fail --silent --show-error "$BASE_URL/api/transfers/$transfer_id" |
            jq --exit-status --raw-output .status
    )"

    if [[ "$transfer_status" == Completed ]]; then
        break
    fi

    if [[ "$transfer_status" == Failed ]]; then
        echo "Transfer $transfer_id failed." >&2
        exit 1
    fi

    sleep 2
done

if [[ "${transfer_status:-}" != Completed ]]; then
    echo "Transfer $transfer_id did not complete within 60 seconds." >&2
    exit 1
fi

curl --fail --silent --show-error "$BASE_URL/api/accounts/$source_id" |
    jq --exit-status '.balance == 375' >/dev/null
curl --fail --silent --show-error "$BASE_URL/api/accounts/$destination_id" |
    jq --exit-status '.balance == 225' >/dev/null

replay_status="$(
    curl --silent --show-error \
        --output "$RESPONSE_FILE" \
        --write-out '%{http_code}' \
        --header 'Content-Type: application/json' \
        --header "Idempotency-Key: ${IDEMPOTENCY_KEY}" \
        --data "$transfer_payload" \
        "$BASE_URL/api/transfers/"
)"

if [[ "$replay_status" != 200 ]] ||
    [[ "$(jq --exit-status --raw-output .id "$RESPONSE_FILE")" != "$transfer_id" ]]; then
    echo "The idempotent replay did not return the original transfer." >&2
    exit 1
fi

echo "Remote smoke test passed."
echo "Source account:      $source_id (balance 375)"
echo "Destination account: $destination_id (balance 225)"
echo "Transfer:            $transfer_id (Completed)"
