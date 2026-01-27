using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar;

/// <summary>
/// Middleware that intercepts index.html responses and injects the GetAvatar client script.
/// </summary>
public class ScriptInjectorMiddleware
{
    private const string ScriptTag = @"<script src=""/GetAvatar/ClientScript""></script>";

    private readonly RequestDelegate _next;
    private readonly ILogger<ScriptInjectorMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptInjectorMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger instance.</param>
    public ScriptInjectorMiddleware(RequestDelegate next, ILogger<ScriptInjectorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request and injects the script into index.html responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!IsIndexHtmlRequest(path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var compressionFeature = context.Features.Get<IHttpResponseBodyFeature>();
        context.Features.Set<IHttpsCompressionFeature>(null);
        context.Request.Headers.Remove("Accept-Encoding");

        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context).ConfigureAwait(false);

        memoryStream.Seek(0, SeekOrigin.Begin);

        var contentType = context.Response.ContentType ?? string.Empty;
        if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            context.Response.Body = originalBodyStream;
            return;
        }

        var body = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync().ConfigureAwait(false);

        if (!body.Contains(ScriptTag, StringComparison.Ordinal))
        {
            var bodyCloseIndex = body.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyCloseIndex != -1)
            {
                body = body.Insert(bodyCloseIndex, ScriptTag + "\n");
                _logger.LogDebug("GetAvatar script injected into index.html response");
            }
        }

        var resultBytes = Encoding.UTF8.GetBytes(body);
        context.Response.Headers.Remove("Content-Encoding");
        context.Response.Body = originalBodyStream;
        context.Response.ContentLength = resultBytes.Length;
        await originalBodyStream.WriteAsync(resultBytes).ConfigureAwait(false);
    }

    private static bool IsIndexHtmlRequest(string path)
    {
        return path.Equals("/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/web/index.html", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/web/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/web", StringComparison.OrdinalIgnoreCase);
    }
}
