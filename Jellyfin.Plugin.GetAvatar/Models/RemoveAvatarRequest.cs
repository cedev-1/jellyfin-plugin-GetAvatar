namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for removing a user's avatar.
    /// </summary>
    public class RemoveAvatarRequest
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}
