#!/usr/bin/env bash

#
# This script simply runs all tests. The output is stored in xml files
# that will be analyzed by the Azure DevOps pipeline.
#

# integration tests
cd /app/tests/integration
dotnet test --no-build --test-adapter-path:. --logger:xunit
cp TestResults/*.xml /output
cd -
