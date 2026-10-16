using Microsoft.EntityFrameworkCore;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Api.Middlewares;
using Dsw2025Tpi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Constants;
using Dsw2025Tpi.Data.Seed;

namespace Dsw2025Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();

        builder.Services.AddDbContext<Dsw2025TpiContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddDbContext<AuthenticateContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password = new PasswordOptions { RequiredLength = 8 };
        })
        .AddEntityFrameworkStores<AuthenticateContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddScoped<IProductManagementService, ProductManagementService>();
        builder.Services.AddScoped<IOrderManagementService, OrderManagementService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddTransient<UserSeeder>();

        //JWT Configuration
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("JWT Key");
        var key = Encoding.UTF8.GetBytes(keyText);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig["Issuer"],
                ValidAudience = jwtConfig["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            // Captura errores de autenticación/autorización
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    throw new AuthenticationException(ErrorMessages.UnauthorizedAccess);
                },
                OnForbidden = context =>
                {
                    throw new AuthorizationException(ErrorMessages.ForbiddenAccess);
                }
            };
        });


        //Swagger con autenticación
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Dsw2025Tpi API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese un token JWT válido. Ejemplo: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });
        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
        });


        var app = builder.Build();

        app.UseMiddleware<ErrorHandlingMiddleware>();


        app.UseSwagger();
        app.UseSwaggerUI();


        app.UseHttpsRedirection();
        app.UseCors(MyAllowSpecificOrigins);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/healthcheck");

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            // Crear o actualizar las tablas de ambos contextos antes de cargar datos
            var db = services.GetRequiredService<Dsw2025TpiContext>();
            await db.Database.MigrateAsync();

            var authDb = services.GetRequiredService<AuthenticateContext>();
            await authDb.Database.MigrateAsync();

            // Seed de customers desde JSON
            await db.SeedCustomersAsync();

            // Seed de usuarios + roles en Identity
            var userSeeder = services.GetRequiredService<UserSeeder>();
            await userSeeder.SeedAsync();
        }

        await app.RunAsync();
    }
}
