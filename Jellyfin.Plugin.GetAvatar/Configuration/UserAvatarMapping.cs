namespace Jellyfin.Plugin.GetAvatar.Configuration
{
    /// <summary>
    /// Represents a user's avatar selection.
    /// </summary>
    public class UserAvatarMapping
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar ID.
        /// </summary>
        public string AvatarId { get; set; } = string.Empty;
    }
}
