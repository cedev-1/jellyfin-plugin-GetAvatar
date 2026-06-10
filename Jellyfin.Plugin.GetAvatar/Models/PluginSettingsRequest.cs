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
    }
}
