FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /build

# See https://github.com/NuGet/Home/issues/13062
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false

COPY global.json .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/KnowledgeBaseServer/KnowledgeBaseServer.csproj ./KnowledgeBaseServer/KnowledgeBaseServer.csproj
RUN dotnet restore ./KnowledgeBaseServer/KnowledgeBaseServer.csproj

COPY src/KnowledgeBaseServer ./KnowledgeBaseServer
RUN dotnet publish ./KnowledgeBaseServer/KnowledgeBaseServer.csproj --no-restore  --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final
WORKDIR /app

# Specify the default volume path recommended in the readme.
ENV DATABASE_PATH=/db/knowledgebase.db

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KnowledgeBaseServer.dll"]
