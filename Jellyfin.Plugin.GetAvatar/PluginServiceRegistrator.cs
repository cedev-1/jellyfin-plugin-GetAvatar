using Jellyfin.Data.Events.Users;
using Jellyfin.Plugin.GetAvatar.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.GetAvatar
{
    /// <summary>
    /// Service registrator for the GetAvatar plugin.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHttpClient();
            serviceCollection.AddSingleton<AvatarService>();
            serviceCollection.AddSingleton<OnlinePackService>();
            serviceCollection.AddSingleton<GravatarService>();
            serviceCollection.AddSingleton<IStartupFilter, ScriptInjectorStartup>();
            serviceCollection.AddHostedService<AvatarValidationService>();
            serviceCollection.AddScoped<IEventConsumer<UserCreatedEventArgs>, UserCreatedAvatarConsumer>();
        }
    }
}
