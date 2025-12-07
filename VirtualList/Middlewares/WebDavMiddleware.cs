using System.Net;
using VirtualList.Datas;

namespace VirtualList.Middlewares;

public class WebDavMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
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
        if (pathSegments.Length < 2 || !Guid.TryParse(pathSegments[1].Trim('/'), out var fileNamespaceId))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            _logger.LogWarning($"用户{name}尝试访问{context.Request.Path}：无效请求");
            return;
        }
        var fileNamespace = mainDb.FileNamespaces.Find(fileNamespaceId);
        if (fileNamespace is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            _logger.LogWarning($"用户{name}尝试访问不存在的命名空间{fileNamespaceId}");
            return;
        }
        if (fileNamespace.OwnerName != name)
            switch (context.Request.Method.ToUpper())
            {
                case "GET":
                case "HEAD":
                case "PROPFIND":
                    mainDb.Entry(fileNamespace)
                        .Collection(fn => fn.ReadAccessUsers)
                        .Load();
                    if (!fileNamespace.ReadAccessUsers.Any(ui => ui.Name == name))
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
                    mainDb.Entry(fileNamespace)
                        .Collection(fn => fn.WriteAccessUsers)
                        .Load();
                    if (!fileNamespace.WriteAccessUsers.Any(ui => ui.Name == name))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return;
                    }
                    break;
                case "POST":
                case "LOCK":
                case "UNLOCK":
                    mainDb.Entry(fileNamespace)
                        .Collection(fn => fn.ReadAccessUsers)
                        .Load();
                    mainDb.Entry(fileNamespace)
                        .Collection(fn => fn.WriteAccessUsers)
                        .Load();
                    if (!(fileNamespace.ReadAccessUsers.Any(ui => ui.Name == name) && fileNamespace.WriteAccessUsers.Any(ui => ui.Name == name)))
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