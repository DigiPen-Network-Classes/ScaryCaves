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
var redisConnectionString = builder.Configuration.GetSection("Redis:ConnectionString").Value;
// TODO remove this when orleans state works ...
var redisConn = ConnectionMultiplexer.Connect(redisConnectionString!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConn);
builder.Services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());


// orleans configuration
// dev: just use local everything for now
// use redis for persistence
/*
var redisOptions = new ConfigurationOptions
{
    EndPoints = { redisConnectionString! },
    AbortOnConnectFail = false,
};
*/
builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddRedisGrainStorageAsDefault(options =>
    {
        options.ConfigurationOptions = new ConfigurationOptions
        {
            EndPoints =
            {
                "localhost:6379"
            },
            AbortOnConnectFail = false,
        };
    });
    //siloBuilder.AddMemoryGrainStorage("ScaryCaves");
});

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

app.Run();
