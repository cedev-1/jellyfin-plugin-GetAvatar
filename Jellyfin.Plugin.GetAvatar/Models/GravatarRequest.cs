namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for importing a user's Gravatar.
    /// </summary>
    public class GravatarRequest
    {
        /// <summary>
        /// Gets or sets the email address linked to the Gravatar. It is only used to compute the Gravatar hash and is never stored.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target user ID (optional, defaults to current user if not provided).
        /// </summary>
        public string? UserId { get; set; }
    }
}
