using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using TJConnector.Postgres;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Services.Contracts;
using TJConnector.StateSystem.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

var connect = builder.Configuration.GetConnectionString("LocalDb");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connect, b => b.MigrationsAssembly("TJConnector.Api")));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("TJConnection:BaseURL") ?? "https://pub-api.mark.tj:5230");
    client.DefaultRequestHeaders.Authorization
        = new AuthenticationHeaderValue("Basic"
        , Convert.ToBase64String(Encoding.ASCII.GetBytes(builder.Configuration.GetValue<string>("TJConnection:Token") ?? "")));
});
builder.Services.AddScoped<GetHttpClient>();
builder.Services.AddScoped<IExternalContainer, ExternalContainerService>();
builder.Services.AddScoped<IExternalEmission, ExternalEmissionService>();
builder.Services.AddScoped<IExternalProduct, ExternalProductService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
