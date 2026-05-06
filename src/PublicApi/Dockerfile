FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . .
WORKDIR "/app/src/PublicApi"

RUN dotnet publish "./PublicApi.csproj" -c Release -o /app/publish -r linux-x64 --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PublicApi.dll"]
