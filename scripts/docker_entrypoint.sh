#!/usr/bin/env bash

#
# This script simply runs all tests. The output is stored in xml files
# that will be analyzed by the Azure DevOps pipeline.
#

if [ "$#" -ne 1 ]; then
	echo "You need to supply at least one argument!"
	exit 1
fi

function integration_tests {
	cd /app/tests/integration
	dotnet test --no-build --test-adapter-path:. --logger:xunit
	cp TestResults/TestResults.xml /output/integration.xml
	cd -
}

function create_nuget {
	cd /app/src/lib
	./create_nuget
	find bin/Release/ -name "*.nupkg" -exec cp {} /output/couchdb.nupkg \;
	cd -
}

case "$1" in 
	test)
		echo "Running integration tests."
		integration_tests
		STATUS=true
		;;
	nuget)
		echo "Creating nuget package."
		create_nuget
		STATUS=true
		;;
	*)
		echo "Unknown commmand: $1"
		STATUS=false
		;;
esac

echo "Contents of /output :"
ls -la /output

if [ "$STATUS" = true ]; then
	echo "Script ran successfully."
	exit 0
else
	echo "Script encountered at least one error."
	exit 1
fi
