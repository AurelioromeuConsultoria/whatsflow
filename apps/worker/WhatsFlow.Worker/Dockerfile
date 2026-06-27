# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copia o repositório inteiro (porque a worker referencia projetos em /src)
COPY . .

# Restaura e publica APENAS a worker (evita restaurar /tests que está em .NET 9)
RUN dotnet restore "WhatsFlow.BackgroundWorker/WhatsFlow.BackgroundWorker.csproj"
RUN dotnet publish "WhatsFlow.BackgroundWorker/WhatsFlow.BackgroundWorker.csproj" -c Release -o /out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /out .

ENTRYPOINT ["dotnet", "WhatsFlow.BackgroundWorker.dll"]