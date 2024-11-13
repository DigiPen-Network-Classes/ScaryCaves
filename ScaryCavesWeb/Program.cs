using Microsoft.AspNetCore.Authentication.Cookies;
using ScaryCavesWeb.Hubs;
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
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = settings.PlayerExpires;
    });
builder.Services.AddAuthorization();

// my (local, web) dependencies:
builder.Services.AddScaryCaveWeb();

// signalR
builder.Services.AddSignalR();

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
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("/gamehub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
