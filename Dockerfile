#
# Since this is a library th
#
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

COPY ./ ./
RUN dotnet restore
RUN dotnet build -c "Debug"

COPY ./scripts/docker_entrypoint.sh ./
RUN mkdir /output && chmod -R 777 /output
RUN ls

ENTRYPOINT [ "/app/docker_entrypoint.sh" ]
