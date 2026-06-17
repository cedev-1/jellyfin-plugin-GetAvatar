namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Information about an online avatar pack available for download.
    /// </summary>
    public class OnlinePackInfo
    {
        /// <summary>
        /// Gets or sets the pack identifier (usually the zip file name without extension).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the pack.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file name of the zip asset.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the download URL of the zip asset.
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the zip asset in bytes.
        /// </summary>
        public long Size { get; set; }
    }
}
