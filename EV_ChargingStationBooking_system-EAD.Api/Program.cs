using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using EV_ChargingStationBooking_system_EAD.Api.Common;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EV Charging API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Type **Bearer {token}**",
        Reference = new OpenApiReference { Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme }
    };
    c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

builder.Services.AddControllers().AddNewtonsoftJson();

// ---------------- Options ----------------
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// ---------------- Mongo & DI ----------------
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IAuthUserRepository, AuthUserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IMyProfileService, MyProfileService>();
builder.Services.AddScoped<IEvOwnerRepository, EvOwnerRepository>();
builder.Services.AddScoped<IEvOwnerService, EvOwnerService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>(); 
builder.Services.AddScoped<IChargingStationRepository, ChargingStationRepository>();
builder.Services.AddScoped<IChargingStationService, ChargingStationService>();

// For dev you can AllowAnyOrigin; for stricter:
builder.Services.AddCors(o => o.AddPolicy("dev", p =>
    p.WithOrigins("http://localhost:5173") // Vite default port
     .AllowAnyHeader()
     .AllowAnyMethod()
));

// ---------------- Auth / JWT ----------------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be a long random string (>=32 chars)");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = false, // <- important (was true before)

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            RoleClaimType = ClaimTypes.Role,                      
            NameClaimType = JwtRegisteredClaimNames.UniqueName     
        };

    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed an initial Backoffice account
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await auth.EnsureSeedBackofficeAsync();

    var ownersRepo = scope.ServiceProvider.GetRequiredService<IEvOwnerRepository>();
    await ownersRepo.CreateIndexesAsync();

    var stationRepo = scope.ServiceProvider.GetRequiredService<IChargingStationRepository>();
    await stationRepo.CreateIndexesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ---------- Middleware ORDER matters ----------
app.UseCors("dev");
app.UseAuthentication();   // <-- MUST be before UseAuthorization
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger", false));
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.MapControllers();

app.Run();
