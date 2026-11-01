namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Result of syncing the Immich profile pictures of the linked users.
    /// </summary>
    public class ImmichProfileSyncResult
    {
        /// <summary>
        /// Gets or sets the number of linked users.
        /// </summary>
        public int LinkedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of users whose profile image was updated.
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of users whose Immich picture did not change since the last sync.
        /// </summary>
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of users without a profile picture in Immich.
        /// </summary>
        public int NoPhotoCount { get; set; }

        /// <summary>
        /// Gets or sets the number of users that could not be synchronized.
        /// </summary>
        public int ErrorCount { get; set; }
    }
}
