using System.Collections.Generic;

namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for saving the Jellyfin ↔ Immich user links.
    /// </summary>
    public class ImmichUserMappingsRequest
    {
        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        public List<ImmichUserMappingItem> Mappings { get; set; } = new List<ImmichUserMappingItem>();
    }
}
