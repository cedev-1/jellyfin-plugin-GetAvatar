using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.GetAvatar.Configuration
{
    /// <summary>
    /// Represents the configuration settings for the GetAvatar plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            AvailableAvatars = new List<AvatarInfo>();
            UserAvatars = new List<UserAvatarMapping>();
        }

        /// <summary>
        /// Gets or sets the list of available avatars.
        /// </summary>
        public List<AvatarInfo> AvailableAvatars { get; set; }

        /// <summary>
        /// Gets or sets the list of user avatar selections.
        /// </summary>
        public List<UserAvatarMapping> UserAvatars { get; set; }
    }
}
