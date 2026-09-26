namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for updating plugin feature settings.
    /// </summary>
    public class PluginSettingsRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether auto-assign is enabled.
        /// </summary>
        public bool EnableAutoAssign { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a newly created user immediately receives a random avatar.
        /// </summary>
        public bool EnableAssignOnUserCreation { get; set; }
    }
}
