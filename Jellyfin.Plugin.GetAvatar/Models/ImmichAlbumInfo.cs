namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// An Immich album.
    /// </summary>
    public class ImmichAlbumInfo
    {
        /// <summary>
        /// Gets or sets the album ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the album name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of assets in the album.
        /// </summary>
        public int AssetCount { get; set; }
    }
}
