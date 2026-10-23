#!/bin/bash
set -e

echo "Pulling ScaryCaves version $VERSION for user $DOCKER_USERNAME"
docker pull $DOCKER_USERNAME/scary_aspnet:$VERSION

echo "Redeploying ScaryCaves version $VERSION"
docker stack deploy --detach=true -c stack.yml -c stack.version.yml scarycaves

echo "Forcing update of ScaryCaves version $VERSION"
docker service update --force scarycaves_scary_aspnet
