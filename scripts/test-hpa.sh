#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="talentai"
SERVICE="talentai"
DURATION="${1:-180}"
RESULTS="results"

mkdir -p "$RESULTS"

echo "Starting HPA test for ${DURATION} seconds"

kubectl get hpa "$SERVICE" -n "$NAMESPACE" \
  > "$RESULTS/34-hpa-before.txt"

kubectl run talentai-load \
  --namespace "$NAMESPACE" \
  --image=busybox:1.36 \
  --restart=Never \
  --command -- sh -c '
    END=$(( $(date +%s) + '"$DURATION"' ))
    while [ "$(date +%s)" -lt "$END" ]; do
      wget -q -O /dev/null http://talentai/ || true
    done
  '

echo "Load generator started"

for i in $(seq 1 24); do
  echo "=== Sample $i ==="

  kubectl get hpa "$SERVICE" -n "$NAMESPACE"
  kubectl get deployment "$SERVICE" -n "$NAMESPACE"

  sleep 10
done | tee "$RESULTS/34-hpa-load-test.txt"

kubectl get hpa "$SERVICE" -n "$NAMESPACE" \
  > "$RESULTS/34-hpa-after.txt"

echo "Load test observation complete"
