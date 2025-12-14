using System.Net;
using VirtualList.Datas;

namespace VirtualList.Middlewares;

public class ShareMiddleware(RequestDelegate next, ConfigData config)
{
    public async Task InvokeAsync(HttpContext context, MainDbContext mainDb)
    {
        if (!context.Request.Path.StartsWithSegments("/share", out _, out var remaining))
        {
            await next(context);
            return;
        }
        if(!context.Request.Method.Equals("GET", StringComparison.CurrentCultureIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            context.Response.Headers.Allow = "GET";
            return;
        }
        if(!Guid.TryParse(remaining.ToString().Trim('/'), out var shareId))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }
        var shareInfo = mainDb.SharedFiles.Find(shareId);
        if(shareInfo is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }
        if(shareInfo.ExpiresAt <= DateTime.Now)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Gone;
            mainDb.SharedFiles.Remove(shareInfo);
            mainDb.SaveChanges();
            return;
        }
        context.Request.Headers["X-Real-IP"] = context.Connection.RemoteIpAddress?.ToString();
        context.Request.Headers["REMOTE-HOST"] = context.Connection.RemoteIpAddress?.ToString();
        context.Request.Headers["X-Forwarded-For"] = $"{context.Connection.RemoteIpAddress?.ToString()}, 127.0.0.1";
        context.Request.Headers.Host = $"127.0.0.1:{config.DavPort}";
        context.Request.Path = shareInfo.RealPath;
        await next(context);
        return;
    }
}

public static class ShareMiddlewareExtensions
{
    public static IApplicationBuilder UseFileShare(
        this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<ShareMiddleware>();
    }
}