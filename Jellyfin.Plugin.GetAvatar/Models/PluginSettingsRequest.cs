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

        /// <summary>
        /// Gets or sets a value indicating whether users can import their Gravatar.
        /// Left unchanged when omitted, so clients that don't know this setting don't turn it off.
        /// </summary>
        public bool? EnableGravatar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Immich album import is enabled. Left unchanged when omitted.
        /// </summary>
        public bool? EnableImmich { get; set; }

        /// <summary>
        /// Gets or sets the Immich server URL. Left unchanged when omitted.
        /// </summary>
        public string? ImmichUrl { get; set; }

        /// <summary>
        /// Gets or sets the Immich API key. Left unchanged when omitted or empty, so saving other settings never erases it.
        /// </summary>
        public string? ImmichApiKey { get; set; }
    }
}
