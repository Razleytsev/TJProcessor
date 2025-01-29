using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using TJConnector.Api.Hubs;
using TJConnector.Postgres;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Services.Contracts;
using TJConnector.StateSystem.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

var progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(progData, "TJConnectorAPI", "servicelog.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

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
builder.Services.AddScoped<CustomHttpClient>();
builder.Services.AddScoped<IExternalContainer, ExternalContainerService>();
builder.Services.AddScoped<IExternalEmission, ExternalEmissionService>();
builder.Services.AddScoped<IExternalProduct, ExternalProductService>();

//builder.Services.AddSignalR();
//builder.Services.AddResponseCompression(opts =>
//{
//    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
//        ["application/octet-stream"]);
//});


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.UseResponseCompression(); 
//app.MapHub<OrderHub>("/orderhub");

app.Run();
