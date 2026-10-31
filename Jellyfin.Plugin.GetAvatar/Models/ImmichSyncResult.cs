namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Combined result of re-importing the Immich albums imported so far.
    /// </summary>
    public class ImmichSyncResult
    {
        /// <summary>
        /// Gets or sets the number of albums synchronized.
        /// </summary>
        public int AlbumCount { get; set; }

        /// <summary>
        /// Gets or sets the number of remembered albums that could not be found in Immich.
        /// </summary>
        public int MissingAlbumCount { get; set; }

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
        /// Gets or sets the number of photos left out because an album exceeds the per-import limit.
        /// </summary>
        public int SkippedCount { get; set; }
    }
}
