namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Result of importing an Immich album.
    /// </summary>
    public class ImmichImportResult
    {
        /// <summary>
        /// Gets or sets the album name.
        /// </summary>
        public string AlbumName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of photos in the album.
        /// </summary>
        public int TotalAssets { get; set; }

        /// <summary>
        /// Gets or sets the number of avatars added to the pool.
        /// </summary>
        public int ImportedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of photos already in the pool.
        /// </summary>
        public int DuplicateCount { get; set; }

        /// <summary>
        /// Gets or sets the number of photos that could not be imported.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the number of photos left out because the album exceeds the per-import limit.
        /// </summary>
        public int SkippedCount { get; set; }
    }
}
