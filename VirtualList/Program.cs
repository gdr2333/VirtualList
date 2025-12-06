using System.Security.Cryptography;
using System.Text;
using VirtualList.Components;
using VirtualList.Datas;

var builder = WebApplication.CreateBuilder(args);

using (var dbc = new MainDbContext(builder.Configuration.GetConnectionString("MainContext"), LoggerFactory.Create((lb) => lb.AddSimpleConsole())))
{
    dbc.Database.EnsureCreated();
    if(!dbc.Users.Any(u => u.UserName == "root"))
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        var passwordRandomByte = new byte[32];
        rng.GetBytes(passwordRandomByte);
        rng.GetBytes(salt);
        var passwordString = Convert.ToBase64String(passwordRandomByte);
        var passwordBytes = Encoding.UTF8.GetBytes(passwordString);
        Console.WriteLine($"root password: {passwordString}");
        var userInfo = new UserInfo()
        {
            UserName = "root",
            PasswordSalt = salt,
            PasswordHash = HMACSHA3_384.HashData(salt, passwordBytes),
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

builder.Services.AddTransient<MainDbContext>((sc) => new(builder.Configuration.GetConnectionString("MainContext"), sc.GetService<ILoggerFactory>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
