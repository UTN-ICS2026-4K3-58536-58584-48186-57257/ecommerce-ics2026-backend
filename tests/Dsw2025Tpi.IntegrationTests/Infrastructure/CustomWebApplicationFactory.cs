using Dsw2025Tpi.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta la API en memoria con entorno "Testing" y una base SQLite en memoria compartida.
/// La conexión <see cref="_connection"/> se mantiene abierta durante toda la vida de la fábrica:
/// una base "Mode=Memory" se destruye cuando se cierra la última conexión.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Clave solo para tests (HS256 requiere >= 32 bytes). No se usa fuera de este proyecto.
    public const string JwtKey = "integration-tests-only-key-0123456789-ABCDEFGHIJ";
    public const string JwtIssuer = "Dsw2025Tpi.IntegrationTests";
    public const string JwtAudience = "Dsw2025Tpi.Api";

    private readonly DbConnection _connection;

    public string ConnectionString { get; }

    public CustomWebApplicationFactory()
    {
        // Nombre único por fábrica: evita que distintas ejecuciones compartan datos.
        ConnectionString = $"Data Source=Dsw2025TpiTests_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        _connection = new SqliteConnection(ConnectionString);
        _connection.Open();

        // El esquema debe existir antes de que arranque la API (el seeding de Program lo usa).
        CreateSchema();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Program.TestingEnvironmentName);

        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:ExpireInMinutes", "60");

        // Menos ruido en la salida de los tests (EF Core loguea cada comando SQL en Information).
        builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning");
    }

    private void CreateSchema()
    {
        // Ambos contextos comparten la misma base, así que EnsureCreated() no sirve para el segundo
        // (vería la base como ya existente). Se crean las tablas de cada modelo explícitamente.
        using var businessContext = new Dsw2025TpiContext(
            new DbContextOptionsBuilder<Dsw2025TpiContext>().UseSqlite(_connection).Options);
        businessContext.GetService<IRelationalDatabaseCreator>().CreateTables();

        using var authContext = new AuthenticateContext(
            new DbContextOptionsBuilder<AuthenticateContext>().UseSqlite(_connection).Options);
        authContext.GetService<IRelationalDatabaseCreator>().CreateTables();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}
