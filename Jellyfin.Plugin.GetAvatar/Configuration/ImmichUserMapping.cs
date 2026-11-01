namespace Jellyfin.Plugin.GetAvatar.Configuration
{
    /// <summary>
    /// Links a Jellyfin user to an Immich user, to sync the Immich profile picture.
    /// </summary>
    public class ImmichUserMapping
    {
        /// <summary>
        /// Gets or sets the Jellyfin user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Immich user ID.
        /// </summary>
        public string ImmichUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hash of the Immich picture applied by the last sync, used to detect changes.
        /// </summary>
        public string LastSyncedHash { get; set; } = string.Empty;
    }
}
