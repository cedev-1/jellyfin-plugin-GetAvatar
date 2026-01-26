using System;

namespace Jellyfin.Plugin.GetAvatar.Configuration
{
    /// <summary>
    /// Represents information about an avatar.
    /// </summary>
    public class AvatarInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the avatar.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the avatar.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file name of the avatar image.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date when the avatar was added.
        /// </summary>
        public DateTime DateAdded { get; set; }
    }
}
