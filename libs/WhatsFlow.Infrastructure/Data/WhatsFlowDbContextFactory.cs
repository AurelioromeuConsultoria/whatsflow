using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WhatsFlow.Infrastructure.Data;

/// <summary>
/// Fábrica usada apenas em design-time pelo `dotnet ef` (migrations).
/// Força o provider Npgsql para que as migrations sejam geradas no dialeto PostgreSQL.
/// A connection string não precisa estar viva para `migrations add` — só para `database update`.
/// Sobrescreva via env var WHATSFLOW_DESIGN_CONNECTION quando for aplicar no banco.
/// </summary>
public sealed class WhatsFlowDbContextFactory : IDesignTimeDbContextFactory<WhatsFlowDbContext>
{
    public WhatsFlowDbContext CreateDbContext(string[] args)
    {
        // Alinha o mapeamento de DateTime ao runtime da API/Worker (que liga este switch),
        // para o snapshot da migration não divergir (timestamp vs timestamptz).
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var connectionString =
            Environment.GetEnvironmentVariable("WHATSFLOW_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=whatsflow;Username=whatsflow;Password=whatsflow";

        var options = new DbContextOptionsBuilder<WhatsFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        // Construtor de 1 argumento usa DefaultTenantContext internamente.
        return new WhatsFlowDbContext(options);
    }
}
