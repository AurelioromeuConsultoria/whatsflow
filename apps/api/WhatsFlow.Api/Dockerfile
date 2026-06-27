# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia tudo do repositório para dentro do container
COPY . .

# Restaura e publica APENAS a API (não mexe em tests)
WORKDIR /src/src/WhatsFlow.API
RUN dotnet restore "WhatsFlow.API.csproj"
RUN dotnet publish "WhatsFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Porta padrão que vamos usar no Coolify
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WhatsFlow.API.dll"]
