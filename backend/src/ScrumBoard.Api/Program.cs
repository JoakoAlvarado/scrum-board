using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ScrumBoard.Application.Ports;
using ScrumBoard.Application.Services;
using ScrumBoard.Infrastructure.Persistence;
using ScrumBoard.Infrastructure.Persistence.Seed;
using ScrumBoard.Infrastructure.Repositories;
using ScrumBoard.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración externa (variables de entorno, sin secretos versionados) ---
// La connection string y los secretos de Jwt/Pepper llegan por variables de entorno
// (ver .env.example) usando la convención de doble guion bajo de ASP.NET Core,
// ej: ConnectionStrings__Default, Jwt__Secret, PasswordHasher__Pepper.
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SeccionConfig));
builder.Services.Configure<PasswordHasherOptions>(builder.Configuration.GetSection(PasswordHasherOptions.SeccionConfig));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default no está configurada.");

builder.Services.AddDbContext<ScrumBoardDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- Puertos / adaptadores (Infrastructure) ---
builder.Services.AddScoped<IUsuarioRepository, EfUsuarioRepository>();
builder.Services.AddScoped<IProyectoRepository, EfProyectoRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// --- Casos de uso (Application) ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProyectoService, ProyectoService>();
builder.Services.AddScoped<IColumnaService, ColumnaService>();
builder.Services.AddScoped<ITareaService, TareaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// --- Autenticación JWT ---
var jwtSection = builder.Configuration.GetSection(JwtOptions.SeccionConfig);
var jwtSecret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");

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
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };

    // Permite pasar el token por query string para el canal de SignalR
    // (el navegador no puede enviar headers en el handshake de WebSocket).
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// --- CORS para el frontend Angular (URL configurable por entorno) ---
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // necesario para el handshake de SignalR
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Los enums (Prioridad, EstadoProyecto) viajan como texto ("Media", "Planificado"),
        // no como número — así el frontend no necesita mapear índices arbitrarios y los
        // payloads son legibles en Swagger/DevTools. Sin esto, System.Text.Json serializa
        // enums como int por defecto, lo cual no coincide con lo que espera Angular.
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Agrega el botón "Authorize" en Swagger UI para poder probar los endpoints
    // protegidos (ej. /api/proyectos) pegando el token que devuelve /api/auth/login.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Pegar únicamente el token (sin el prefijo 'Bearer ')."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Migraciones automáticas + seed al arrancar (agiliza el levantamiento en Docker) ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScrumBoardDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context, passwordHasher);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ScrumBoard.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
