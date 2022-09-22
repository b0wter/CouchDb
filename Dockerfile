#
# Since this is a library th
#
FROM mcr.microsoft.com/dotnet/sdk:6.0-buster-slim
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
