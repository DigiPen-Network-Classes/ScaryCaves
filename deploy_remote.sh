#!/bin/bash
set -e

echo "SSH-Deploy ScaryCaves version $VERSION"
echo "SSH_AUTH_SOCK is $SSH_AUTH_SOCK"

scp -v -r ./stack.yml $DEPLOY_USERNAME@scarycaves.meancat.com:/home/$DEPLOY_USERNAME

ssh -v $DEPLOY_USERNAME@scarycaves.meancat.com <<"EOF"
  echo "Pulling ScaryCaves version $VERSION"
  docker pull $DOCKER_USERNAME/scary_aspnet:$VERSION
  docker pull $DOCKER_USERNAME/scary_next:$VERSION

  echo "Redeploying ScaryCaves version $VERSION"
  docker stack deploy --detach=true -c stack.yml scarycaves

  echo "Forcing update of ScaryCaves version $VERSION"
  docker service update --force scarycaves_scary_aspnet
  docker service update --force scarycaves_scary_next
EOF
