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
            EnableAssignOnUserCreation = true;
            // Albums stay on by default so the import keeps working for installs that enabled Immich before this option existed
            EnableImmichAlbums = true;
            ImmichUrl = string.Empty;
            ImmichApiKey = string.Empty;
            ImmichSyncAlbumIds = new List<string>();
            ImmichUserMappings = new List<ImmichUserMapping>();
        }

        /// <summary>
        /// Gets or sets the list of available avatars.
        /// </summary>
        public List<AvatarInfo> AvailableAvatars { get; set; }

        /// <summary>
        /// Gets or sets the list of user avatar selections.
        /// </summary>
        public List<UserAvatarMapping> UserAvatars { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether new users without an avatar automatically receive a random one at startup.
        /// </summary>
        public bool EnableAutoAssign { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a newly created user immediately receives a random avatar.
        /// </summary>
        public bool EnableAssignOnUserCreation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether users can import their Gravatar as profile image.
        /// </summary>
        public bool EnableGravatar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Immich album import is enabled.
        /// </summary>
        public bool EnableImmich { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Immich album import and sync is enabled (when Immich is enabled).
        /// </summary>
        public bool EnableImmichAlbums { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Jellyfin ↔ Immich user links and profile picture sync is enabled (when Immich is enabled).
        /// </summary>
        public bool EnableImmichUsers { get; set; }

        /// <summary>
        /// Gets or sets the base URL of the Immich server used to import album photos as avatars.
        /// </summary>
        public string ImmichUrl { get; set; }

        /// <summary>
        /// Gets or sets the Immich API key. Stored in clear in the plugin configuration and never sent back to clients.
        /// </summary>
        public string ImmichApiKey { get; set; }

        /// <summary>
        /// Gets or sets the IDs of the Immich albums imported so far, re-imported by the "Sync" button.
        /// </summary>
        public List<string> ImmichSyncAlbumIds { get; set; }

        /// <summary>
        /// Gets or sets the links between Jellyfin users and Immich users, used to sync profile pictures.
        /// </summary>
        public List<ImmichUserMapping> ImmichUserMappings { get; set; }
    }
}
