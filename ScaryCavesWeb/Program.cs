using Microsoft.AspNetCore.Authentication.Cookies;
using ScaryCavesWeb.Controllers;
using ScaryCavesWeb.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// settings
var settings = new ScaryCaveSettings();
builder.Configuration.Bind("ScaryCaves", settings);
builder.Services.AddSingleton(settings);

// redis connection
var redisHost = builder.Configuration.GetSection("Redis:Host").Value;
var redisPort = builder.Configuration.GetSection("Redis:Port").Value;
var redisConn = ConnectionMultiplexer.Connect($"{redisHost}:{redisPort}");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConn);
builder.Services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

// simple cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = settings.PlayerExpires;
    });
builder.Services.AddAuthorization();

// my (local, web) dependencies:
builder.Services.AddScaryCaveWeb();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// WebSockets support
var wsOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseWebSockets(wsOptions);
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var ws = await context.WebSockets.AcceptWebSocketAsync();
            await WebSocketHandler.HandleWebSocketAsync(context, ws);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// places, everyone
await app.Services.GetRequiredService<WorldDatabase>().Initialize();

app.Run();
