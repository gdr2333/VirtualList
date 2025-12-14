using System.Net;
using VirtualList.Datas;

namespace VirtualList.Middlewares;

public class WebDavMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, ConfigData config)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<WebDavMiddleware>();

    public async Task InvokeAsync(HttpContext context, MainDbContext mainDb)
    {
        if (!context.Request.Path.StartsWithSegments("/webdav"))
        {
            await next(context);
            return;
        }
        var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var name = context.User.Identity?.Name;
        if (name is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }
        if (pathSegments.Length < 2 || !Guid.TryParse(pathSegments[1].Trim('/'), out var FileSpaceId))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            _logger.LogWarning($"用户{name}尝试访问{context.Request.Path}：无效请求");
            return;
        }
        var FileSpace = mainDb.FileSpaces.Find(FileSpaceId);
        if (FileSpace is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            _logger.LogWarning($"用户{name}尝试访问不存在的命名空间{FileSpaceId}");
            return;
        }
        if (FileSpace.OwnerName != name)
            switch (context.Request.Method.ToUpper())
            {
                case "GET":
                case "HEAD":
                case "PROPFIND":
                    mainDb.Entry(FileSpace)
                        .Collection(fn => fn.ReadAccessUsers)
                        .Load();
                    if (!FileSpace.ReadAccessUsers.Any(ui => ui.Name == name))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return;
                    }
                    break;
                case "PUT":
                case "DELETE":
                case "PROPPATCH":
                case "MKCOL":
                case "COPY":
                case "MOVE":
                    mainDb.Entry(FileSpace)
                        .Collection(fn => fn.WriteAccessUsers)
                        .Load();
                    if (!FileSpace.WriteAccessUsers.Any(ui => ui.Name == name))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return;
                    }
                    FileSpace.LastModifiedAt = DateTime.Now;
                    mainDb.SaveChanges();
                    break;
                case "POST":
                case "LOCK":
                case "UNLOCK":
                    mainDb.Entry(FileSpace)
                        .Collection(fn => fn.ReadAccessUsers)
                        .Load();
                    mainDb.Entry(FileSpace)
                        .Collection(fn => fn.WriteAccessUsers)
                        .Load();
                    if (!(FileSpace.ReadAccessUsers.Any(ui => ui.Name == name) && FileSpace.WriteAccessUsers.Any(ui => ui.Name == name)))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return;
                    }
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return;
            }
        // YARP不支持请求头替换，写这好了
        /* 
location / {
  proxy_pass http://127.0.0.1:8080;
  proxy_set_header X-Real-IP $remote_addr;
  proxy_set_header REMOTE-HOST $remote_addr;
  proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
  proxy_set_header Host $host;
  proxy_redirect off;

  # Ensure COPY and MOVE commands work. Change https://example.com to the
  # correct address where the WebDAV server will be deployed at.
  set $dest $http_destination;
  if ($http_destination ~ "^https://example.com(?<path>(.+))") {
    set $dest /$path;
  }
  proxy_set_header Destination $dest;
}
        */
        context.Request.Headers["X-Real-IP"] = context.Connection.RemoteIpAddress?.ToString();
        context.Request.Headers["REMOTE-HOST"] = context.Connection.RemoteIpAddress?.ToString();
        context.Request.Headers["X-Forwarded-For"] = $"{context.Connection.RemoteIpAddress?.ToString()}, 127.0.0.1";
        context.Request.Headers.Host = config.DavPort.ToString();
        var dest = context.Request.Headers["Destination"].ToString();
        context.Request.Headers["Destination"] = dest[(dest.IndexOf('/') + 1)..];
        // 让我们把剩下的代理流程交给YARP
        await next(context);
        return;
    }
}

public static class WebDavMiddlewareExtensions
{
    public static IApplicationBuilder UseWebDav(
        this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<WebDavMiddleware>();
    }
}