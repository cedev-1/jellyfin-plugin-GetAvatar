namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// A link between a Jellyfin user and an Immich user, as sent by the admin page.
    /// </summary>
    public class ImmichUserMappingItem
    {
        /// <summary>
        /// Gets or sets the Jellyfin user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Immich user ID. An empty value means the user is not linked.
        /// </summary>
        public string ImmichUserId { get; set; } = string.Empty;
    }
}
