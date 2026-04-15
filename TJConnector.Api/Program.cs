using MassTransit;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using TJConnector.Api.Services;
using TJConnector.Api.TestRun;
using TJConnector.Api.Transit;
using TJConnector.Api.TransitBatches;
using TJConnector.Postgres;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Services.Contracts;
using TJConnector.StateSystem.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

var progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.File(Path.Combine(progData, "TJConnectorAPI", "servicelog.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

var connect = builder.Configuration.GetConnectionString("LocalDb");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connect, b => b.MigrationsAssembly("TJConnector.Api")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    }); 

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
builder.Services.AddSingleton<ISQLConnectionFactory, SQLConnectionFactory>();
builder.Services.AddScoped<IExternalDBData, ExternalDbData>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITestRunService, TestRunService>();

builder.Services.AddMassTransit(cfg =>
{
    cfg.SetInMemorySagaRepositoryProvider();
    cfg.UsingInMemory((context, config) =>
    {
        config.ConfigureEndpoints(context);
    });

    cfg.AddConsumer<StateCheckSSCC>();
    cfg.AddConsumer<ExternalDbCheck>();
    cfg.AddConsumer<ExternalDbContent>();
    cfg.AddConsumer<StateCreateApplication>();
    cfg.AddConsumer<StateApplicationStatus>();
    cfg.AddConsumer<StateProcessApplication>();
    cfg.AddConsumer<StateCreateAggregation>();
    cfg.AddConsumer<StateAggregationStatus>();
    cfg.AddConsumer<StateProcessAggregation>();
    cfg.AddConsumer<ReprocessConsumer>();
    cfg.AddConsumer<BatchInitialConsumer>();
    cfg.AddConsumer<CreateOrdersConsumer>();
    cfg.AddConsumer<ProcessOrdersConsumer>();

    cfg.AddConsumer<EmitPacksConsumer>();
    cfg.AddConsumer<EmitBundlesConsumer>();
    cfg.AddConsumer<EmitMastercaseConsumer>();
    cfg.AddConsumer<SubmitApplicationConsumer>();
    cfg.AddConsumer<SubmitAggregationConsumer>();
});

//builder.Services.AddSignalR();
//builder.Services.AddResponseCompression(opts =>
//{
//    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
//        ["application/octet-stream"]);
//});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

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
