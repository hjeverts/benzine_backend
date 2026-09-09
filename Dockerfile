FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Vehictory.Api/Vehictory.Api.csproj ./Vehictory.Api/
RUN dotnet restore ./Vehictory.Api/Vehictory.Api.csproj

COPY src/Vehictory.Api/. ./Vehictory.Api/
WORKDIR /src/Vehictory.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Vehictory.Api.dll"]
