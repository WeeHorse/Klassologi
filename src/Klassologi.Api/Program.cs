using System.Text.Json.Serialization;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Classes;
using Klassologi.Api.Features.Status;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp => new Db(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Klassologi")
    ?? throw new InvalidOperationException(
        "Anslutningssträngen 'ConnectionStrings:Klassologi' saknas. Se README.md.")));

// Inloggningslösningen läggs till i steg 3 (Q2). Tills dess finns inget schema och alla anrop är anonyma.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

Migrations.Run(app.Services.GetRequiredService<Db>().ConnectionString, app.Logger);

app.UseAuthentication();
app.UseAuthorization();

app.MapStatusEndpoints();
app.MapClassEndpoints();

app.Run();
