using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VirtualList.Datas;

namespace VirtualList.Middlewares;

public partial class AuthenticationHandler(MainDbContext mainDb, ILoggerFactory loggerFactory) : IAuthenticationHandler
{
    private HttpContext _context;
    private ILogger logger = loggerFactory.CreateLogger<AuthenticationHandler>();

    public async Task<AuthenticateResult> AuthenticateAsync()
    {
        foreach (var cookiePart in _context.Request.Headers.Cookie)
            foreach (var cookie in cookiePart?.Split(';') ?? [])
                if (cookie?.StartsWith("Token=") ?? false)
                {
                    var loginInfo = mainDb.LoginInfos.Find(Convert.FromBase64String(cookie[6..].Trim()));
                    if (loginInfo != null && loginInfo.ExpiresAt > DateTime.Now)
                    {
                        mainDb.Entry(loginInfo)
                            .Reference(li => li.User)
                            .Load();
                        if (logger.IsEnabled(LogLevel.Debug))
                            logger.LogDebug($"对{loginInfo.User.Name}验证成功，使用Cookie:Token");
                        return AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, loginInfo.User.Name)], "cookie")) , "cookie"));
                    }
                }
        // 只对WebDAV路径开放Basic认证
        if (_context.Request.Path.StartsWithSegments("/webdav"))
        {
            var auth = _context.Request.Headers.Authorization.ToString();
            if (auth.StartsWith("Basic "))
            {
                string decodedAuth = Encoding.UTF8.GetString(Convert.FromBase64String(auth[6..].Trim()));
                string[] authValues = [decodedAuth[..decodedAuth.IndexOf(':')], decodedAuth[decodedAuth.IndexOf(':')..]];
                var userInfo = mainDb.Users.Find(authValues[0]);
                if (userInfo is not null && userInfo.PasswordHash.SequenceEqual(HMACSHA3_384.HashData(userInfo.PasswordSalt, Encoding.UTF8.GetBytes(authValues[1]))))
                {
                    logger.LogDebug($"对{userInfo.Name}验证成功，使用Basic");
                    userInfo.LastLogin = DateTime.Now;
                    mainDb.SaveChanges();
                    return AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, userInfo.Name)], "basic")), "basic"));
                }
            }
        }
        return AuthenticateResult.Fail("请登录（网页用户）或使用Basic验证输入用户名和密码（WebDAV用户）");
    }

    public async Task ChallengeAsync(AuthenticationProperties? properties)
    {
        if (_context.Request.Path.StartsWithSegments("/webdav"))
        {
            _context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            _context.Response.Headers.Append("WWW-Authenticate", "basic realm=\"WebDAV需要Basic认证\"");
        }
        else
        {
            _context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            _context.Response.Headers.Append("Location", "/login");
        }
        return;
    }

    public async Task ForbidAsync(AuthenticationProperties? properties)
    {
        _context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        return;
    }

    public async Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        _context = context;
    }
}
