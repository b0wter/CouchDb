#!/usr/bin/env bash

[ -z "$1" ] && echo "You need to supply the hostname as first parameter." && exit 1
[ -z "$2" ] && echo "You need to supply the port as second parameter." && exit 1

HOST="$1"
PORT="$2"

STATUSCODE=$(curl -o /dev/null --silent --head --write-out '%{http_code}\n' http://$HOST:$PORT/_up)

while [ "$STATUSCODE" -ne 200 ]
do
	printf '%s' "."
	sleep 10
	STATUSCODE=$(curl -o /dev/null --silent --head --write-out '%{http_code}\n' http://localhost:5984/_up)
done

echo "CouchDb is now available!"
