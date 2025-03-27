using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using ScaryCavesWeb.Hubs;
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
builder.Services
    .AddOptions<ScaryCaveSettings>()
    .BindConfiguration("ScaryCave")
    .ValidateDataAnnotations();

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/Login";
        options.ExpireTimeSpan = scaryCaveSettings.AccountExpires;
    });
builder.Services.AddAuthorization();

// my (local, web) dependencies:
builder.Services.AddScaryCaveWeb();

// signalR
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        // send enums as strings
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// cors for scarycave-spa
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", b => b.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// This gets activated by the runtime before we start processing http requests,
// but after the Orleans Silos are operational
// "Places, Everyone ..."
builder.Services.AddHostedService<WorldInitializerHostedService>();

// Add controllers to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("/GameHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
