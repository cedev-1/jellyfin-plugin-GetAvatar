namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for testing an Immich connection, possibly with credentials that are not saved yet.
    /// </summary>
    public class ImmichTestRequest
    {
        /// <summary>
        /// Gets or sets the Immich URL to test. The saved URL is used when empty.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the API key to test. The saved key is used when empty.
        /// </summary>
        public string? ApiKey { get; set; }
    }
}
