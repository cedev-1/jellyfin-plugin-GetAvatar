using System.Text.RegularExpressions;
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
        private const string StartComment = "<!-- GetAvatar Plugin Start -->";
        private const string EndComment = "<!-- GetAvatar Plugin End -->";

        private readonly ILogger<Plugin> _logger;
        private readonly IApplicationPaths _appPaths;

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
            _appPaths = applicationPaths;
            Instance = this;
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

        /// <inheritdoc />
        public override void OnUninstalling()
        {
            RemoveInjectedScript();
            base.OnUninstalling();
        }

        /// <summary>
        /// Removes the injected script from index.html when plugin is uninstalled.
        /// </summary>
        private void RemoveInjectedScript()
        {
            try
            {
                var indexPath = Path.Combine(_appPaths.WebPath, "index.html");

                if (!File.Exists(indexPath))
                {
                    return;
                }

                var content = File.ReadAllText(indexPath);

                var regex = new Regex($"{Regex.Escape(StartComment)}[\\s\\S]*?{Regex.Escape(EndComment)}", RegexOptions.Multiline);
                var newContent = regex.Replace(content, string.Empty);

                if (newContent != content)
                {
                    File.WriteAllText(indexPath, newContent);
                    _logger.LogInformation("GetAvatar script removed from index.html");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove GetAvatar script from index.html");
            }
        }
    }
}
