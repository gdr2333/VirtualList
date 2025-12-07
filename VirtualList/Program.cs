using Microsoft.FluentUI.AspNetCore.Components;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VirtualList.Components;
using VirtualList.Datas;
using VirtualList.Helpers;
using VirtualList.Middlewares;
using VirtualList.Services;
using Yarp.ReverseProxy.Configuration;

var conf = JsonSerializer.Deserialize<ConfigData>(File.ReadAllBytes("Config.json"))!;

if(!Directory.Exists(conf.StroageAt))
    Directory.CreateDirectory(conf.StroageAt);

File.WriteAllText("DavServerConfig-AutoGen.yaml", $"address: 127.0.0.1\nport: {conf.DavPort}\ntls: false\nprefix: /\ndebug: false\nnoSniff: false\nbehindProxy: true\ndirectory: {conf.StroageAt}\npermissions: CRUD\nrules: []\nrulesBehavior: overwrite\nlog:\n  format: console\n  colors: true\n  outputs:\n  - stderr\nnoPassword: true");

var builder = WebApplication.CreateBuilder(args);

using (var dbc = new MainDbContext(builder.Configuration.GetConnectionString("MainContext"), LoggerFactory.Create((lb) => lb.AddSimpleConsole())))
{
    dbc.Database.EnsureCreated();
    if (!dbc.Users.Any(u => u.Name == "root"))
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        var passwordRandomByte = new byte[32];
        rng.GetBytes(passwordRandomByte);
        rng.GetBytes(salt);
        var passwordString = Convert.ToBase64String(passwordRandomByte);
        Console.WriteLine($"root password: {passwordString}");
        var userInfo = new UserInfo()
        {
            Name = "root",
            PasswordSalt = salt,
            PasswordHash = PasswordHelper.HashPassword(salt, passwordString),
            CreatedTime = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            LoginInfos = [],
            OwnNamespaces = [],
            ReadableNamespaces = [],
            WriteableNamespaces = [],
            SharedFiles = []
        };
        dbc.Users.Add(userInfo);
        dbc.SaveChanges();
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(oa =>
{
    oa.DefaultAuthenticateScheme = "cookie";
    oa.DefaultChallengeScheme = "cookie";
    oa.AddScheme<AuthenticationHandler>("cookie", "Cookie");
    oa.AddScheme<AuthenticationHandler>("basic", "Basic");
});

builder.Services.AddTransient<MainDbContext>((sc) => new(builder.Configuration.GetConnectionString("MainContext")!, sc.GetService<ILoggerFactory>()!));

builder.Services.AddSingleton(conf);

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        [
        new()
        {
            RouteId = "webdav-route",
            ClusterId = "main-cluster",
            Match = new()
            {
                Path = "/webdav/{**catch-all}"
            },
            Transforms =
            [
                new Dictionary<string, string>()
                {
                    ["RequestHeader"] = "Host",
                    ["Set"] = "{HttpContext.Request.Host.Value}"
                },
                new Dictionary<string, string>
                {
                    ["RequestHeader"] = "X-Real-IP",
                    ["Set"] = "{HttpContext.Connection.RemoteIpAddress}"
                },
                new Dictionary<string, string>
                {
                    ["RequestHeader"] = "REMOTE-HOST",
                    ["Set"] = "{HttpContext.Connection.RemoteIpAddress}"
                }
            ]
        }
        ],
        [
            new()
            {
                ClusterId = "main-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        "webdev", new DestinationConfig
                        {
                            Address = $"http://127.0.0.1:{conf.DavPort}/"
                        }
                    }
                }
            }
        ]
    );

builder.Services.AddHostedService<WebDavServerService>();

builder.Services.AddFluentUIComponents();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseWebDav();
app.MapReverseProxy();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
