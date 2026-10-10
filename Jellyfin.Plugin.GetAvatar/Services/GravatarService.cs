using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Downloads avatars from Gravatar.
    /// </summary>
    public class GravatarService
    {
        private const int AvatarSize = 512;
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GravatarService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public GravatarService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Computes the Gravatar hash of an email address: SHA-256 of the trimmed, lowercased address.
        /// See https://docs.gravatar.com/avatars/hash.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <returns>The lowercase hexadecimal hash.</returns>
        public static string GetEmailHash(string email)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Downloads the Gravatar of an email address.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <returns>The image data and its file extension, or null if the address has no Gravatar.</returns>
        /// <exception cref="HttpRequestException">Gravatar could not be reached or returned an error.</exception>
        /// <exception cref="InvalidDataException">Gravatar did not return a supported image, or the image is too large.</exception>
        public async Task<(byte[] Data, string Extension)?> DownloadAvatarAsync(string email)
        {
            // d=404 makes Gravatar answer 404 instead of a placeholder when the address has no avatar
            var url = $"https://gravatar.com/avatar/{GetEmailHash(email)}?s={AvatarSize}&d=404";

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Jellyfin-Plugin-GetAvatar");
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var extension = response.Content.Headers.ContentType?.MediaType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                var other => throw new InvalidDataException($"Unsupported Gravatar content type: {other}")
            };

            if (response.Content.Headers.ContentLength > MaxImageSizeBytes)
            {
                throw new InvalidDataException("Gravatar image exceeds the maximum allowed size.");
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
                    throw new InvalidDataException("Gravatar image exceeds the maximum allowed size.");
                }
            }

            return (buffer.ToArray(), extension);
        }
    }
}
