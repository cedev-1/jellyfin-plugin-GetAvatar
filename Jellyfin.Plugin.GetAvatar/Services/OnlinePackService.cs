using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.GetAvatar.Controllers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Service for discovering and importing online avatar packs from GitHub releases.
    /// </summary>
    public class OnlinePackService
    {
        private const string GitHubReleaseApiUrl = "https://api.github.com/repos/cedev-1/jellyfin-avatars/releases/latest";
        private const long MaxZipSizeBytes = 1024L * 1024L * 1024L; // 1 GB

        private static readonly Dictionary<string, string> PackDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "dp", "Disney Plus" },
            { "nf", "Netflix" },
            { "one", "Xbox One" },
            { "playstation", "PlayStation" },
            { "pp", "Paramount Plus" },
            { "pv", "Prime Video" },
            { "sonny", "Sonny Angels" },
            { "steam", "Steam" }
        };

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxImageSizeBytes = 5L * 1024L * 1024L;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AvatarService _avatarService;
        private readonly ILogger<OnlinePackService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlinePackService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="logger">The logger instance.</param>
        public OnlinePackService(
            IHttpClientFactory httpClientFactory,
            AvatarService avatarService,
            ILogger<OnlinePackService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _avatarService = avatarService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the available avatar packs from the latest GitHub release.
        /// </summary>
        /// <returns>List of available packs.</returns>
        public async Task<List<OnlinePackInfo>> GetAvailablePacksAsync()
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Jellyfin-Plugin-GetAvatar");
            client.Timeout = TimeSpan.FromSeconds(30);

            _logger.LogInformation("Fetching online avatar packs from {Url}", GitHubReleaseApiUrl);

            var response = await client.GetAsync(GitHubReleaseApiUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var packs = new List<OnlinePackInfo>();

            if (!root.TryGetProperty("assets", out var assets))
            {
                _logger.LogWarning("No assets found in GitHub release response");
                return packs;
            }

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (string.IsNullOrEmpty(name) || !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var id = Path.GetFileNameWithoutExtension(name);
                var downloadUrl = asset.GetProperty("browser_download_url").GetString();
                var size = asset.TryGetProperty("size", out var sizeProperty) ? sizeProperty.GetInt64() : 0;

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    continue;
                }

                packs.Add(new OnlinePackInfo
                {
                    Id = id,
                    Name = GetDisplayName(id),
                    FileName = name,
                    DownloadUrl = downloadUrl,
                    Size = size
                });
            }

            _logger.LogInformation("Found {Count} online avatar packs", packs.Count);
            return packs;
        }

        /// <summary>
        /// Imports the specified online avatar packs.
        /// </summary>
        /// <param name="packIds">The pack identifiers to import.</param>
        /// <returns>The import result.</returns>
        public async Task<ImportOnlinePacksResult> ImportPacksAsync(List<string> packIds)
        {
            if (packIds == null || packIds.Count == 0)
            {
                return new ImportOnlinePacksResult();
            }

            var availablePacks = await GetAvailablePacksAsync().ConfigureAwait(false);
            var selectedPacks = availablePacks.Where(p => packIds.Contains(p.Id, StringComparer.OrdinalIgnoreCase)).ToList();

            var result = new ImportOnlinePacksResult();

            foreach (var pack in selectedPacks)
            {
                try
                {
                    _logger.LogInformation("Importing online avatar pack: {PackName} ({FileName})", pack.Name, pack.FileName);
                    var details = await ImportPackAsync(pack).ConfigureAwait(false);
                    result.ImportedCount += details.ImportedCount;
                    result.TotalImages += details.TotalImages;
                    result.DuplicateCount += details.DuplicateCount;
                    result.PackResults.Add(new PackImportResult
                    {
                        PackId = pack.Id,
                        PackName = pack.Name,
                        ImportedCount = details.ImportedCount,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import online avatar pack: {PackName}", pack.Name);
                    result.PackResults.Add(new PackImportResult
                    {
                        PackId = pack.Id,
                        PackName = pack.Name,
                        ImportedCount = 0,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return result;
        }

        private async Task<PackImportDetails> ImportPackAsync(OnlinePackInfo pack)
        {
            var existingHashes = GetExistingAvatarHashes();
            var tempDirectory = Path.Combine(Path.GetTempPath(), "GetAvatar", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var zipPath = Path.Combine(tempDirectory, pack.FileName);
                await DownloadFileAsync(pack.DownloadUrl, zipPath, MaxZipSizeBytes).ConfigureAwait(false);

                var extractDirectory = Path.Combine(tempDirectory, "extracted");
                Directory.CreateDirectory(extractDirectory);
                ZipFile.ExtractToDirectory(zipPath, extractDirectory);

                _logger.LogInformation("Extracted pack {PackName} to {ExtractDirectory}. Looking for images...", pack.Name, extractDirectory);

                var allFiles = Directory.GetFiles(extractDirectory, "*.*", SearchOption.AllDirectories).ToList();
                _logger.LogInformation("Found {Count} total files in pack {PackName}", allFiles.Count, pack.Name);

                var imageFiles = allFiles
                    .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                _logger.LogInformation("Found {Count} image files in pack {PackName}", imageFiles.Count, pack.Name);

                var details = new PackImportDetails
                {
                    TotalImages = imageFiles.Count
                };

                foreach (var imagePath in imageFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(imagePath);
                        if (fileInfo.Length > MaxImageSizeBytes)
                        {
                            _logger.LogWarning("Skipping image {FileName}: exceeds {MaxSize} MB", imagePath, MaxImageSizeBytes / (1024 * 1024));
                            continue;
                        }

                        var imageData = await File.ReadAllBytesAsync(imagePath).ConfigureAwait(false);
                        var hash = ComputeHash(imageData);

                        if (existingHashes.Contains(hash))
                        {
                            details.DuplicateCount++;
                            _logger.LogDebug("Skipping duplicate image: {FileName}", Path.GetFileName(imagePath));
                            continue;
                        }

                        await _avatarService.SaveAvatarAsync(Path.GetFileName(imagePath), imageData, pack.Name).ConfigureAwait(false);
                        existingHashes.Add(hash);
                        details.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to import image {FileName} from pack {PackName}", imagePath, pack.Name);
                    }
                }

                _logger.LogInformation("Imported {ImportedCount} avatars from pack {PackName} ({DuplicateCount} duplicates skipped)", details.ImportedCount, pack.Name, details.DuplicateCount);
                return details;
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary directory: {Path}", tempDirectory);
                }
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath, long maxSizeBytes)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxSizeBytes)
            {
                throw new InvalidOperationException($"File size ({contentLength.Value} bytes) exceeds maximum allowed size ({maxSizeBytes} bytes).");
            }

            var totalBytesRead = 0L;
            using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                totalBytesRead += bytesRead;

                if (totalBytesRead > maxSizeBytes)
                {
                    throw new InvalidOperationException($"Downloaded file exceeded maximum allowed size ({maxSizeBytes} bytes).");
                }
            }
        }

        private HashSet<string> GetExistingAvatarHashes()
        {
            var hashes = new HashSet<string>();
            var avatarDirectory = _avatarService.AvatarDirectory;

            if (!Directory.Exists(avatarDirectory))
            {
                return hashes;
            }

            foreach (var file in Directory.GetFiles(avatarDirectory))
            {
                try
                {
                    var data = File.ReadAllBytes(file);
                    hashes.Add(ComputeHash(data));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to compute hash for existing avatar: {File}", file);
                }
            }

            return hashes;
        }

        private static string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash);
        }

        private static string GetDisplayName(string packId)
        {
            return PackDisplayNames.TryGetValue(packId, out var displayName)
                ? displayName
                : char.ToUpperInvariant(packId[0]) + packId.Substring(1);
        }

        /// <summary>
        /// Result of importing online avatar packs.
        /// </summary>
        public class ImportOnlinePacksResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ImportOnlinePacksResult"/> class.
            /// </summary>
            public ImportOnlinePacksResult()
            {
                PackResults = new List<PackImportResult>();
            }

            /// <summary>
            /// Gets or sets the total number of avatars imported.
            /// </summary>
            public int ImportedCount { get; set; }

            /// <summary>
            /// Gets or sets the total number of image files found in selected packs.
            /// </summary>
            public int TotalImages { get; set; }

            /// <summary>
            /// Gets or sets the total number of duplicate images skipped.
            /// </summary>
            public int DuplicateCount { get; set; }

            /// <summary>
            /// Gets or sets the detailed results for each pack.
            /// </summary>
            public List<PackImportResult> PackResults { get; set; }
        }

        private class PackImportDetails
        {
            public int ImportedCount { get; set; }

            public int TotalImages { get; set; }

            public int DuplicateCount { get; set; }
        }

        /// <summary>
        /// Result of importing a single online avatar pack.
        /// </summary>
        public class PackImportResult
        {
            /// <summary>
            /// Gets or sets the pack identifier.
            /// </summary>
            public string PackId { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the pack display name.
            /// </summary>
            public string PackName { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the number of avatars imported from this pack.
            /// </summary>
            public int ImportedCount { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the import succeeded.
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Gets or sets the error message if the import failed.
            /// </summary>
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}
