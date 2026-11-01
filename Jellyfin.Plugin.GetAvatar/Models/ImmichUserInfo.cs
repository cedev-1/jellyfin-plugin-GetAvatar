namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// An Immich user.
    /// </summary>
    public class ImmichUserInfo
    {
        /// <summary>
        /// Gets or sets the Immich user ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
