using Klassologi.Api.Data;
using Klassologi.Api.Features.Status;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp => new Db(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Klassologi")
    ?? throw new InvalidOperationException(
        "Anslutningssträngen 'ConnectionStrings:Klassologi' saknas. Se README.md.")));

var app = builder.Build();

Migrations.Run(app.Services.GetRequiredService<Db>().ConnectionString, app.Logger);

app.MapStatusEndpoints();

app.Run();
