#!/usr/bin/env bash

if [ "$#" -eq 0 ]; then
	echo "Usage: up-test.sh <host> <port> [command]"
	echo ""
	echo "All arguments after <port> will be used to run as a new comand."
	echo "This is useful if you want to send a message or beep after the database is available."
	exit
fi

[ -z "$1" ] && echo "You need to supply the hostname as first parameter." && exit 1
[ -z "$2" ] && echo "You need to supply the port as second parameter." && exit 1

HOST="$1"
PORT="$2"
COMMAND="${@:3}"

STATUSCODE=$(curl -o /dev/null --silent --head --write-out '%{http_code}\n' http://$HOST:$PORT/_up)

while [ "$STATUSCODE" -ne 200 ]
do
	printf '%s' "."
	sleep 10
	STATUSCODE=$(curl -o /dev/null --silent --head --write-out '%{http_code}\n' http://localhost:5984/_up)
done

echo "CouchDb is now available!"

if [ -z "$COMMAND" ]; then
	# nothing to do
	NOTHING="NOTHING"
else
	$COMMAND
fi
