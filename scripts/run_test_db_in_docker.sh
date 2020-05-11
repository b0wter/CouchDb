#!/usr/bin/env bash
docker run -it --rm -e COUCHDB_USER=admin -e COUCHDB_PASSWORD=password --name temp-couchdb -p 5985:5984 --security-opt=seccomp=unconfined registry.hub.docker.com/library/couchdb:latest
