using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// ── Authentication / Authorization ──────────────────────────────
// Cookie-based, mirrors the Changer reference: /login + /logout
// endpoints, /access-denied for authenticated-but-forbidden, 7-day
// sliding cookie. Users are still hardcoded (same as Changer).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5166";
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
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

// In container / HTTP-only deployments HTTPS isn't bound; only redirect when
// we actually have an HTTPS endpoint configured.
if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORTS"])
    || !string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ── /api/login (POST form) — cookie auth requires HTTP headers, so the
//    sign-in cannot ride on the Blazor WebSocket circuit. Login.razor
//    submits a native HTML form to this endpoint.
app.MapPost("/api/login", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    // Hardcoded user table — same shape as the Changer reference.
    var users = new Dictionary<string, (string Password, string Role)>
    {
        ["user"]  = ("user123",     "User"),
        ["admin"] = ("Secret12345", "Admin")
    };

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)
        || !users.TryGetValue(username, out var entry) || entry.Password != password)
    {
        var safeReturn = string.IsNullOrWhiteSpace(returnUrl) ? "" : $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect($"/login?error=1{safeReturn}");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, username),
        new(ClaimTypes.Role, entry.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
}).AllowAnonymous();

// ── /logout ─────────────────────────────────────────────────────
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
