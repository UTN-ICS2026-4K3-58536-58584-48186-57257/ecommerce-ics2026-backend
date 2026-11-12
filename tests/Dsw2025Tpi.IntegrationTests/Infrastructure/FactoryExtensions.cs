using System.Net.Http.Headers;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Helpers para crear tokens JWT y clientes HTTP autenticados contra la API levantada por
/// <see cref="CustomWebApplicationFactory"/>.
/// </summary>
public static class FactoryExtensions
{
    /// <summary>Nombre de usuario ficticio que se pone como "sub" en los tokens de prueba.</summary>
    public const string TestUserName = "integration-test-user";

    /// <summary>
    /// Genera un JWT firmado con la configuración de la fábrica para un usuario de prueba con el rol indicado.
    /// Los roles de la API son "admin" y "customer" (en minúsculas).
    /// </summary>
    public static string CreateToken(this CustomWebApplicationFactory factory, string role)
    {
        var tokenService = factory.Services.GetRequiredService<JwtTokenService>();
        return tokenService.GenerateToken(TestUserName, role);
    }

    /// <summary>
    /// Crea un <see cref="HttpClient"/> con BaseAddress "https://localhost" y sin seguir redirecciones.
    /// Si <paramref name="role"/> no es null, agrega "Authorization: Bearer {token}" para ese rol.
    /// </summary>
    public static HttpClient CreateApiClient(this CustomWebApplicationFactory factory, string? role = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        if (role is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", factory.CreateToken(role));
        }

        return client;
    }
}
