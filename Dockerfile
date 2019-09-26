#
# Since this is a library th
#
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

COPY ./*.sln ./
COPY src ./src
COPY tests ./tests
COPY .git ./.git
RUN dotnet restore
RUN dotnet build -c "Debug"
RUN dotnet build -c "Release"

COPY ./scripts/docker_entrypoint.sh ./
RUN mkdir /output && chmod -R 777 /output

ENTRYPOINT [ "/app/docker_entrypoint.sh" ]
