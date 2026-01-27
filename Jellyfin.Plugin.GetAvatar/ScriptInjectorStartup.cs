using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.GetAvatar;

/// <summary>
/// Startup filter that registers the script injector middleware in the ASP.NET pipeline.
/// </summary>
public class ScriptInjectorStartup : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<ScriptInjectorMiddleware>();
            next(app);
        };
    }
}
