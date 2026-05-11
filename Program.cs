using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MaisonGlace.API.Services;
using MaisonGlace.API.Settings;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ── Bind settings ──────────────────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// ── Singleton services ────────────────────────────────────────────────────
builder.Services.AddSingleton<DatabaseContext>();
builder.Services.AddSingleton<BookingService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<AuthService>();

// ── JWT authentication ────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── CORS (allow Next.js dev server + production domain) ────────────────────
var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

// ── Startup diagnostics for Render logs ───────────────────────────────────
var envName = app.Environment.EnvironmentName;
var mongoConn = app.Configuration["MongoDb:ConnectionString"] ?? string.Empty;
var mongoConfigured = !string.IsNullOrWhiteSpace(mongoConn);
var mongoTarget = "not-configured";

if (mongoConfigured)
{
    try
    {
        var mongoUrl = MongoUrl.Create(mongoConn);
        var host = mongoUrl.Server?.ToString() ?? "unknown-host";
        var db = string.IsNullOrWhiteSpace(mongoUrl.DatabaseName) ? "unknown-db" : mongoUrl.DatabaseName;
        mongoTarget = $"{host}/{db}";
    }
    catch
    {
        mongoTarget = "invalid-connection-string";
    }
}

app.Logger.LogInformation("Startup environment: {Environment}", envName);
app.Logger.LogInformation("Mongo configured: {MongoConfigured}; target: {MongoTarget}", mongoConfigured, mongoTarget);
app.Logger.LogInformation("AllowedOrigins: {AllowedOrigins}", string.Join(",", allowedOrigins));

// ── Seed admin account on startup ──────────────────────────────────────────
var authService = app.Services.GetRequiredService<AuthService>();
try
{
    await authService.SeedAdminAsync(
        app.Configuration["Admin:Email"] ?? "abizmichelle@gmail.com",
        app.Configuration["Admin:Password"] ?? "Bisola1369");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Admin seed skipped: unable to connect to MongoDB during startup.");
}

// ── Global exception diagnostics for Render logs ──────────────────────────
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled exception. Path: {Path}; Method: {Method}",
            context.Request.Path,
            context.Request.Method);
        throw;
    }
});

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

// Render.com sets the PORT env var; fall back to 8080 for local Docker
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
