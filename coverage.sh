#!/bin/bash

output=`dotnet test --collect:"XPlat Code Coverage" | sed -n '/Attachments:/{n;p}' | sed -e 's/^\s*//'`

if [ -f TestResults/coverage ]; then
    rm -rf TestResults/coverage
fi

reportgenerator -reports:$output -targetdir:TestResults/coverage

firefox TestResults/coverage/index.html