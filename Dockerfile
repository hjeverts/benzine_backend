FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Benzine.Api/Benzine.Api.csproj ./Benzine.Api/
RUN dotnet restore ./Benzine.Api/Benzine.Api.csproj

COPY src/Benzine.Api/. ./Benzine.Api/
WORKDIR /src/Benzine.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Benzine.Api.dll"]
