#!/bin/bash
set -e

echo "Pulling ScaryCaves version $VERSION for user $DOCKER_USERNAME"
docker pull $DOCKER_USERNAME/scary_aspnet:$VERSION
docker pull $DOCKER_USERNAME/scary_next:$VERSION

echo "Redeploying ScaryCaves version $VERSION"
docker stack deploy --detach=true -c stack.yml scarycaves

echo "Forcing update of ScaryCaves version $VERSION"
docker service update --force scarycaves_scary_aspnet
docker service update --force scarycaves_scary_next
