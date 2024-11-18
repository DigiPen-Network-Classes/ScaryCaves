#!/bin/bash
set -e 
docker build -t scary_aspnet ./ScaryCavesWeb
docker build -t scary_next ./scarycaves-next
