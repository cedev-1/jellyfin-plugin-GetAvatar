using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// Request model for importing online avatar packs.
    /// </summary>
    public class ImportOnlinePacksRequest
    {
        /// <summary>
        /// Gets or sets the list of pack identifiers to import.
        /// </summary>
        [JsonPropertyName("packs")]
        public List<string> PackIds { get; set; } = new List<string>();
    }
}
