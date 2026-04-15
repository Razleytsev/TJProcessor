using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Serilog;
using TJConnector.Web.Services.Contracts;
using TJConnector.Web.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 512;
    });

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5166")
});

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderServiceWeb>();
builder.Services.AddScoped<IMetadataService, MetadataService>();
builder.Services.AddScoped<IBatchServiceWeb, BatchServiceWeb>();
builder.Services.AddScoped<IPackageRequestService, PackageRequestService>();
builder.Services.AddScoped<ITestRunServiceWeb, TestRunServiceWeb>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/batches");
        return;
    }
    await next();
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
