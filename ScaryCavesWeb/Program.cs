using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using ScaryCavesWeb.Components;
using ScaryCavesWeb.Endpoints;
using ScaryCavesWeb.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// setup configuration files:
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
if (builder.Environment.IsDevelopment())
{
    var localUser = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.LocalUser.json");
    if (File.Exists(localUser))
    {
        builder.Configuration.AddJsonFile(localUser, optional: true, reloadOnChange: true);
    }
}
builder.Configuration.AddEnvironmentVariables();

var scaryCaveSettings = new ScaryCaveSettings();
builder.Configuration.Bind("ScaryCave", scaryCaveSettings);
builder.Services.AddSingleton(scaryCaveSettings);

builder.Services.AddScaryDataProtection(scaryCaveSettings);

// used for recaptcha:
builder.Services.AddHttpClient();

builder.Host.UseOrleans(static siloBuilder =>
{
    // re-fetch the settings for orleans:
    var scaryCaveSettings = new ScaryCaveSettings();
    siloBuilder.Configuration.Bind("ScaryCave", scaryCaveSettings);

    // orleans configures its own logging:
    siloBuilder.ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddSimpleConsole(options =>
        {
            options.UseUtcTimestamp = true;
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ "; // UTC timestamp format
        });
    });

    siloBuilder.UseLocalhostClustering();
    // Accounts and Players expire after "config seconds"
    siloBuilder.AddRedisGrainStorage(ScaryCaveSettings.AccountStorageProvider, options =>
    {
        options.EntryExpiry = scaryCaveSettings.AccountExpires;
        options.ConfigurationOptions = ConfigurationOptions.Parse(scaryCaveSettings.RedisConnectionString);
    });

    // everything else is stored in redis without expiration (and it doesn't grow over time)
    siloBuilder.AddRedisGrainStorageAsDefault(options =>
    {
        options.ConfigurationOptions = ConfigurationOptions.Parse(scaryCaveSettings.RedisConnectionString);
    });
});

// simple cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        // Secure whenever the request is: nginx terminates TLS and forwards the scheme,
        // while plain-http local development still gets a cookie.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = scaryCaveSettings.AccountExpires;
    });
builder.Services.AddAuthorization();

// my (local, web) dependencies:
builder.Services.AddScaryCaveWeb();

// nginx on the same host terminates TLS; trust its X-Forwarded-* headers so the app
// sees https (secure cookies) and the real client address.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// This gets activated by the runtime before we start processing http requests,
// but after the Orleans Silos are operational
// "Places, Everyone ..."
builder.Services.AddHostedService<WorldInitializerHostedService>();

// Static SSR only: no AddInteractiveServerComponents(), no @rendermode, no blazor.web.js.
builder.Services.AddRazorComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// No UseHttpsRedirection: nginx terminates TLS and already redirects port 80 to https.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();
app.MapAccountEndpoints();
app.MapCaveEndpoints();

app.Run();
