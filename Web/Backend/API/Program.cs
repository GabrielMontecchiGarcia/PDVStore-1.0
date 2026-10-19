using System.Text;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PDVStore.API.Common;
using PDVStore.API.Configuration;
using PDVStore.API.Security;
using PDVStore.DTOs;
using PDVStore.Middleware;
using PDVStore.Repository.Context;
using Serilog;

// ---------------------------------------------------------------------------
// Logging estruturado (Serilog): console + arquivo diário com retenção de 14 dias,
// equivalente ao log em arquivo do sistema Windows.
// ---------------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pdvstore-web-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Fuso horário usado nos relatórios (substitui o ToLocalTime da máquina).
    var fusoId = builder.Configuration["App:TimeZone"] ?? "America/Sao_Paulo";
    TimeZoneInfo fuso;
    try
    {
        fuso = TimeZoneInfo.FindSystemTimeZoneById(fusoId);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Fuso {Fuso} indisponível; usando UTC.", fusoId);
        fuso = TimeZoneInfo.Utc;
    }

    // JWT: chave obrigatória (mínimo 32 caracteres) vinda de configuração/segredo.
    var jwtSection = builder.Configuration.GetSection("Jwt");
    builder.Services.Configure<JwtOptions>(jwtSection);
    var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

    if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        throw new InvalidOperationException(
            "Jwt:Key não configurada ou com menos de 32 caracteres. Use a variável de ambiente Jwt__Key.");

    // MVC + validação automática (FluentValidation) + Swagger + DI.
    builder.Services.AddControllers();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PDVStore Web API",
            Version = "v1",
            Description = "API REST do PDVStore — mesma lógica de negócio do sistema Windows."
        });

        var scheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Informe apenas o token JWT (sem o prefixo Bearer).",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };

        options.AddSecurityDefinition("Bearer", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { scheme, Array.Empty<string>() }
        });
    });

    builder.Services.AddPdvStore(builder.Configuration, fuso);

    // Respostas de validação padronizadas em PT-BR (mesmo formato do middleware).
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var detalhes = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .ToList();

            return new BadRequestObjectResult(new ErroResposta
            {
                Status = StatusCodes.Status400BadRequest,
                Mensagem = "Um ou mais campos estão inválidos.",
                Detalhes = detalhes,
                TraceId = context.HttpContext.TraceIdentifier
            });
        };
    });

    // Proteção contra força bruta no login (10 tentativas por minuto por IP).
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    // Autenticação JWT + policies por perfil (matriz de acesso do menu original).
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Somente Administrador (usuários, caixa, compras, fornecedores, relatórios).
        options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));

        // PDV: Administrador e Operador (Caixa).
        options.AddPolicy("Pdv", policy => policy.RequireRole("Administrador", "Operador"));

        // Produtos: Administrador e Estoquista.
        options.AddPolicy("Produtos", policy => policy.RequireRole("Administrador", "Estoquista"));
    });

    // CORS restrito às origens configuradas (frontend React).
    var origens = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                  ?? new[] { "http://localhost:5173" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod());
    });

    var app = builder.Build();

    // Pipeline HTTP: erros globais, logs, CORS, headers de segurança e auth.
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    // Arquivos estáticos (fotos de usuário gravadas em wwwroot/uploads).
    app.UseStaticFiles();

    app.Use(async (context, next) =>
    {
        // Cabeçalhos de segurança (XSS/clickjacking/sniffing).
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        await next();
    });

    app.UseCors("Frontend");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "ok",
        sistema = "PDVStore Web API",
        utc = DateTime.UtcNow
    })).AllowAnonymous();

    // Verificação de banco equivalente aos passos 2 e 3 da splash original
    // (conexão + migrations pendentes), sem expor a connection string.
    app.MapGet("/health/database", async (IServiceProvider services) =>
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PDVStoreDbContext>();

        try
        {
            var conectado = await db.Database.CanConnectAsync();
            if (!conectado)
            {
                return Results.Json(new
                {
                    conectado = false,
                    mensagem = "Não foi possível conectar ao banco de dados."
                }, statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            int? migrationsPendentes = null;
            try
            {
                migrationsPendentes = (await db.Database.GetPendingMigrationsAsync()).Count();
            }
            catch
            {
                // Provider sem suporte a migrations (ex.: InMemory/demonstração).
            }

            return Results.Ok(new
            {
                conectado = true,
                provider = db.Database.ProviderName,
                migrationsPendentes,
                mensagem = migrationsPendentes is > 0
                    ? $"{migrationsPendentes} migração(ões) pendente(s)."
                    : "Banco de dados atualizado."
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new
            {
                conectado = false,
                mensagem = "Falha na verificação do banco de dados.",
                detalhe = ex.GetType().Name
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }).AllowAnonymous();

    // Aplicação opcional das migrations no startup (equivalente ao passo 3 da splash).
    var providerBanco = builder.Configuration["Database:Provider"] ?? "SqlServer";

    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        if (providerBanco.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("ApplyMigrationsOnStartup ignorado: provider InMemory em uso (somente desenvolvimento).");
        }
        else
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PDVStoreDbContext>();
            try
            {
                await db.Database.MigrateAsync();
                Log.Information("Migrations aplicadas com sucesso.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao aplicar migrations no startup.");
                throw;
            }
        }
    }

    // Dados de demonstração apenas no modo InMemory (nunca no SQL Server).
    if (providerBanco.Equals("InMemory", StringComparison.OrdinalIgnoreCase) &&
        builder.Configuration.GetValue<bool>("Database:SeedDemoData"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PDVStoreDbContext>();
        await db.Database.EnsureCreatedAsync();
        await PDVStore.API.Data.DbInitializer.SemearAsync(db);
        Log.Information("Banco InMemory criado e populado com dados de demonstração.");
    }

    Log.Information("PDVStore Web API iniciada.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao iniciar a API");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Classe parcial exposta para os testes de integração (WebApplicationFactory),
/// permitindo sobrescrever a configuração da API em tempo de teste.
/// </summary>
public partial class Program
{
}
