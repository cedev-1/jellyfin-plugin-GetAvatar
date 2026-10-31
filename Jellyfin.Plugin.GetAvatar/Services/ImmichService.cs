using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Imports album photos from an Immich server as avatars.
    /// Only needs an API key with the <c>album.read</c>, <c>asset.read</c> and <c>asset.view</c> permissions.
    /// </summary>
    public class ImmichService
    {
        private const int MaxImagesPerAlbum = 500;
        private const int SearchPageSize = 100;
        private const long MaxImageSizeBytes = 5L * 1024L * 1024L;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AvatarService _avatarService;
        private readonly ILogger<ImmichService> _logger;
        private readonly SemaphoreSlim _importLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmichService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="logger">The logger instance.</param>
        public ImmichService(
            IHttpClientFactory httpClientFactory,
            AvatarService avatarService,
            ILogger<ImmichService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _avatarService = avatarService;
            _logger = logger;
        }

        /// <summary>
        /// Normalizes the URL of an Immich server to its API base URL.
        /// </summary>
        /// <param name="url">The URL entered by the administrator.</param>
        /// <returns>The API base URL (no trailing slash, ending with <c>/api</c>), or null if the URL is not a valid http(s) URL.</returns>
        public static string? NormalizeUrl(string? url)
        {
            var trimmed = url?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(trimmed)
                || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return trimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase) ? trimmed : trimmed + "/api";
        }

        /// <summary>
        /// Lists the albums visible to the API key. Uses the saved URL and key unless overrides are given,
        /// which lets the administrator test credentials before saving them.
        /// </summary>
        /// <param name="urlOverride">A URL to use instead of the saved one.</param>
        /// <param name="apiKeyOverride">An API key to use instead of the saved one.</param>
        /// <returns>The albums.</returns>
        /// <exception cref="InvalidOperationException">Immich is not configured.</exception>
        /// <exception cref="HttpRequestException">Immich could not be reached or returned an error.</exception>
        public async Task<List<ImmichAlbumInfo>> ListAlbumsAsync(string? urlOverride = null, string? apiKeyOverride = null)
        {
            using var client = CreateClient(out var baseUrl, urlOverride, apiKeyOverride);
            using var response = await client.GetAsync(baseUrl + "/albums").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
            var albums = new List<ImmichAlbumInfo>();
            foreach (var album in document.RootElement.EnumerateArray())
            {
                albums.Add(new ImmichAlbumInfo
                {
                    Id = album.GetProperty("id").GetString() ?? string.Empty,
                    Name = album.TryGetProperty("albumName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                    AssetCount = album.TryGetProperty("assetCount", out var count) && count.TryGetInt32(out var parsed) ? parsed : 0
                });
            }

            return albums;
        }

        /// <summary>
        /// Imports the photos of an album into the avatar pool, under a category named after the album.
        /// Additive only: photos already in the pool (same content) are skipped and nothing is ever deleted.
        /// </summary>
        /// <param name="albumId">The Immich album ID.</param>
        /// <returns>The import result.</returns>
        /// <exception cref="InvalidOperationException">Immich is not configured, or the album does not exist.</exception>
        /// <exception cref="HttpRequestException">Immich could not be reached or returned an error.</exception>
        public async Task<ImmichImportResult> ImportAlbumAsync(string albumId)
        {
            await _importLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var albums = await ListAlbumsAsync().ConfigureAwait(false);
                var album = albums.Find(a => string.Equals(a.Id, albumId, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Album not found");

                return await ImportAlbumCoreAsync(album).ConfigureAwait(false);
            }
            finally
            {
                _importLock.Release();
            }
        }

        /// <summary>
        /// Re-imports every album imported so far, to pick up the photos added since. Same rules as
        /// <see cref="ImportAlbumAsync"/>: additive only, duplicates skipped, nothing deleted.
        /// Albums that no longer exist (or are no longer visible to the API key) are counted, not removed from the list.
        /// </summary>
        /// <returns>The combined result.</returns>
        /// <exception cref="InvalidOperationException">Immich is not configured.</exception>
        /// <exception cref="HttpRequestException">Immich could not be reached or returned an error.</exception>
        public async Task<ImmichSyncResult> SyncAlbumsAsync()
        {
            await _importLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var syncResult = new ImmichSyncResult();
                var albumIds = new List<string>(Plugin.Config.ImmichSyncAlbumIds ?? new List<string>());
                if (albumIds.Count == 0)
                {
                    return syncResult;
                }

                var albums = await ListAlbumsAsync().ConfigureAwait(false);
                foreach (var albumId in albumIds)
                {
                    var album = albums.Find(a => string.Equals(a.Id, albumId, StringComparison.OrdinalIgnoreCase));
                    if (album == null)
                    {
                        syncResult.MissingAlbumCount++;
                        continue;
                    }

                    var result = await ImportAlbumCoreAsync(album).ConfigureAwait(false);
                    syncResult.AlbumCount++;
                    syncResult.ImportedCount += result.ImportedCount;
                    syncResult.DuplicateCount += result.DuplicateCount;
                    syncResult.ErrorCount += result.ErrorCount;
                    syncResult.SkippedCount += result.SkippedCount;
                }

                return syncResult;
            }
            finally
            {
                _importLock.Release();
            }
        }

        private async Task<ImmichImportResult> ImportAlbumCoreAsync(ImmichAlbumInfo album)
        {
            using var client = CreateClient(out var baseUrl);
            var result = new ImmichImportResult { AlbumName = album.Name };
            var assets = await ListAlbumAssetsAsync(client, baseUrl, album.Id, result).ConfigureAwait(false);
            var existingHashes = _avatarService.GetExistingAvatarHashes();

            foreach (var (assetId, fileName) in assets)
            {
                try
                {
                    var image = await DownloadThumbnailAsync(client, baseUrl, assetId).ConfigureAwait(false);
                    var hash = AvatarService.ComputeHash(image.Data);
                    if (!existingHashes.Add(hash))
                    {
                        result.DuplicateCount++;
                        continue;
                    }

                    var name = Path.GetFileNameWithoutExtension(fileName);
                    await _avatarService.SaveAvatarAsync((string.IsNullOrWhiteSpace(name) ? assetId : name) + image.Extension, image.Data, album.Name).ConfigureAwait(false);
                    result.ImportedCount++;
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or TaskCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to import Immich asset {AssetId}", assetId);
                    result.ErrorCount++;
                }
            }

            RememberAlbum(album.Id);

            _logger.LogInformation(
                "Imported {Imported} avatars from Immich album {Album} ({Duplicates} duplicates, {Errors} errors, {Skipped} skipped)",
                result.ImportedCount,
                album.Name,
                result.DuplicateCount,
                result.ErrorCount,
                result.SkippedCount);
            return result;
        }

        private static void RememberAlbum(string albumId)
        {
            var config = Plugin.Config;
            config.ImmichSyncAlbumIds ??= new List<string>();
            if (!config.ImmichSyncAlbumIds.Contains(albumId, StringComparer.OrdinalIgnoreCase))
            {
                config.ImmichSyncAlbumIds.Add(albumId);
                Plugin.Instance?.SaveConfiguration();
            }
        }

        private static async Task<List<(string Id, string FileName)>> ListAlbumAssetsAsync(HttpClient client, string baseUrl, string albumId, ImmichImportResult result)
        {
            var assets = new List<(string, string)>();
            var total = 0;
            string? page = "1";

            while (page != null && assets.Count < MaxImagesPerAlbum)
            {
                var body = JsonSerializer.Serialize(new { albumIds = new[] { albumId }, type = "IMAGE", page = int.Parse(page, System.Globalization.CultureInfo.InvariantCulture), size = SearchPageSize });
                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/search/metadata")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
                var section = document.RootElement.GetProperty("assets");
                if (section.TryGetProperty("total", out var totalElement) && totalElement.TryGetInt32(out var parsedTotal))
                {
                    total = parsedTotal;
                }

                foreach (var item in section.GetProperty("items").EnumerateArray())
                {
                    if (assets.Count >= MaxImagesPerAlbum)
                    {
                        break;
                    }

                    assets.Add((
                        item.GetProperty("id").GetString() ?? string.Empty,
                        item.TryGetProperty("originalFileName", out var fileName) ? fileName.GetString() ?? string.Empty : string.Empty));
                }

                page = section.TryGetProperty("nextPage", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
            }

            result.TotalAssets = Math.Max(total, assets.Count);
            result.SkippedCount = result.TotalAssets - assets.Count;
            return assets;
        }

        private static async Task<(byte[] Data, string Extension)> DownloadThumbnailAsync(HttpClient client, string baseUrl, string assetId)
        {
            // "preview" is a reduced-size image; "fullsize" may redirect to the original, which needs another permission
            using var response = await client.GetAsync(
                $"{baseUrl}/assets/{Uri.EscapeDataString(assetId)}/thumbnail?size=preview",
                HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var extension = response.Content.Headers.ContentType?.MediaType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                var other => throw new InvalidDataException($"Unsupported Immich content type: {other}")
            };

            if (response.Content.Headers.ContentLength > MaxImageSizeBytes)
            {
                throw new InvalidDataException("Immich image exceeds the maximum allowed size.");
            }

            await using var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(chunk).ConfigureAwait(false)) > 0)
            {
                buffer.Write(chunk, 0, bytesRead);
                if (buffer.Length > MaxImageSizeBytes)
                {
                    throw new InvalidDataException("Immich image exceeds the maximum allowed size.");
                }
            }

            return (buffer.ToArray(), extension);
        }

        private HttpClient CreateClient(out string baseUrl, string? urlOverride = null, string? apiKeyOverride = null)
        {
            var config = Plugin.Config;
            var url = string.IsNullOrWhiteSpace(urlOverride) ? config.ImmichUrl : urlOverride;
            var apiKey = string.IsNullOrWhiteSpace(apiKeyOverride) ? config.ImmichApiKey : apiKeyOverride.Trim();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Enter the Immich URL and API key first");
            }

            baseUrl = NormalizeUrl(url) ?? throw new InvalidOperationException("The Immich URL must be a valid http(s) URL");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json, image/*");
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }
    }
}
