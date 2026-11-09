#!/usr/bin/env bash
set -euo pipefail

IMAGE_REPO="fedimriri/talentai"
TAG="${1:?Usage: ./scripts/deploy-local.sh IMAGE_TAG}"

helm upgrade talentai ./helm/talentai \
  --namespace talentai \
  --reuse-values \
  --set image.repository="$IMAGE_REPO" \
  --set-string image.tag="$TAG" \
  --set image.pullPolicy=IfNotPresent \
  --wait \
  --timeout 5m

kubectl rollout status deployment/talentai \
  --namespace talentai \
  --timeout=180s

kubectl get deployments,pods,hpa \
  --namespace talentai

echo "Deployment complete: ${IMAGE_REPO}:${TAG}"
