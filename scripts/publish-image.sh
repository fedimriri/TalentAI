#!/usr/bin/env bash
set -euo pipefail

IMAGE_REPO="fedimriri/talentai"
TAG="${1:-$(date -u +%Y%m%d%H%M%S)}"
IMAGE="${IMAGE_REPO}:${TAG}"
TRIVY="${HOME}/.local/bin/trivy"

if ! docker image inspect "$IMAGE" >/dev/null 2>&1; then
  echo "Building ${IMAGE}"
  docker build --pull --tag "$IMAGE" .
else
  echo "Reusing existing image: ${IMAGE}"
fi

echo "Scanning ${IMAGE}"

"$TRIVY" image \
  --severity HIGH,CRITICAL \
  --exit-code 1 \
  "$IMAGE"

echo "Publishing ${IMAGE}"

docker push "$IMAGE"

echo "Published: ${IMAGE}"
