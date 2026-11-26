using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Klassologi.Api.Auth;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Classes;
using Klassologi.Api.Features.Invitations;
using Klassologi.Api.Features.PasswordResets;
using Klassologi.Api.Features.Status;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp => new Db(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Klassologi")
    ?? throw new InvalidOperationException(
        "Anslutningssträngen 'ConnectionStrings:Klassologi' saknas. Se README.md.")));

// Autentisering (Q2, Q16): ASP.NET Core Identity med egen lagring och sessionscookie.
builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.Password.RequiredLength = Accounts.MinPasswordLength;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredUniqueChars = 1;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.Lockout.AllowedForNewUsers = true;
        o.User.AllowedUserNameCharacters = ""; // Användarnamnet är e-postadressen, som valideras separat.
        o.ClaimsIdentity.UserIdClaimType = "sub";
    })
    .AddUserStore<UserStore>()
    .AddSignInManager();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "klassologi.session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
    // API:t omdirigerar aldrig till en inloggningssida.
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
// Säkerhetsstämpeln kontrolleras vid varje anrop, så att utloggning och nytt lösenord gäller alla sessioner direkt.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.AddAuthorization();

// Begränsning av anropstakt för inloggning, aktivering och återställning, per IP-adress.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(AuthEndpoints.RateLimitPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "okänd",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = ctx.RequestServices.GetRequiredService<IConfiguration>().GetValue("Auth:RateLimitPerMinute", 20),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

var db = app.Services.GetRequiredService<Db>();
Migrations.Run(db.ConnectionString, app.Logger);

// Q15: den första administratören. Körs från servern, aldrig via webben.
if (args is ["create-admin-invitation", ..])
{
    var link = await Invitations.CreateAdministratorInvitationAsync(db);
    var baseUrl = app.Configuration.GetValue("App:PublicBaseUrl", "http://localhost:5173")!.TrimEnd('/');
    Console.WriteLine($"Inbjudan för administratör, giltig till {link.ExpiresAt:yyyy-MM-dd HH:mm} UTC:");
    Console.WriteLine(baseUrl + link.Url);
    return;
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// API-dokumentation och testgränssnitt bara under utveckling: /openapi/v1.json och /scalar.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Klassologi API"));
}

app.MapStatusEndpoints();
app.MapAuthEndpoints();
app.MapClassEndpoints();
app.MapInvitationEndpoints();
app.MapPasswordResetEndpoints();

app.Run();
