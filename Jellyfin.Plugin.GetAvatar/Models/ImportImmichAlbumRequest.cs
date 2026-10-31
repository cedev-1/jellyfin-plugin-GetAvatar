namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for importing an Immich album.
    /// </summary>
    public class ImportImmichAlbumRequest
    {
        /// <summary>
        /// Gets or sets the Immich album ID.
        /// </summary>
        public string AlbumId { get; set; } = string.Empty;
    }
}
