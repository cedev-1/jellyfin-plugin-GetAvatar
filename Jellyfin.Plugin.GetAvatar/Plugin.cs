using Jellyfin.Plugin.GetAvatar.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar
{
    /// <summary>
    /// Main plugin class for GetAvatar that handles configuration and web pages.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILogger<Plugin> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="logger">The logger instance.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            _logger = logger;
            Instance = this;

            _logger.LogInformation("GetAvatar Plugin initialized");
        }

        /// <summary>
        /// Gets the logger instance for the plugin.
        /// </summary>
        public static ILogger<Plugin> Logger => Instance!._logger;

        /// <summary>
        /// Gets the plugin configuration.
        /// </summary>
        public static PluginConfiguration Config => Instance!.Configuration;

        /// <inheritdoc />
        public override string Name => "GetAvatar";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("88accc81-d913-44b3-b1d3-2abfa457dd2d");

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            // Admin configuration page (Dashboard → Plugins → GetAvatar)
            yield return new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.configPage.html"
            };
            yield return new PluginPageInfo
            {
                Name = "GetAvatarConfig",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.configPage.js"
            };

            // User avatar selection page (User Settings → Avatar)
            yield return new PluginPageInfo
            {
                Name = "GetAvatarUserPage",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.Web.userAvatarPage.html",
                DisplayName = "Avatar",
                MenuSection = "user",
                MenuIcon = "person"
            };
        }
    }
}
