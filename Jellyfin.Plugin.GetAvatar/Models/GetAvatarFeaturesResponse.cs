using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Response model for the optional features, the client API version and the plugin version.
    /// </summary>
    public class GetAvatarFeaturesResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether users can import their Gravatar.
        /// </summary>
        [JsonPropertyName("gravatarEnabled")]
        public bool GravatarEnabled { get; set; }

        /// <summary>
        /// Gets or sets the version of the client API, see <see cref="Plugin.ApiVersion"/>.
        /// </summary>
        [JsonPropertyName("apiVersion")]
        public int ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets the plugin version, or an empty string when the plugin is not initialized.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }
}
