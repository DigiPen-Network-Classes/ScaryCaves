#!/bin/bash
set -e
# build for x86_64 instead
docker buildx build --platform linux/amd64 -t trasa/scary_aspnet:latest ./ScaryCavesWeb

docker buildx build --platform linux/amd64 \
  --build-arg NEXT_PUBLIC_BASE_URL=https://scarycaves.meancat.com \
  --build-arg NEXT_PUBLIC_API_BASE_URL=https://scarycaves.meancat.com/api \
  -t trasa/scary_next:latest ./scarycaves-next
