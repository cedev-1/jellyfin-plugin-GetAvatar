using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.GetAvatar.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Service for managing user avatars.
    /// </summary>
    public class AvatarService
    {
        private readonly IUserManager _userManager;
        private readonly IProviderManager _providerManager;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<AvatarService> _logger;
        private readonly string _avatarDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="logger">The logger instance.</param>
        public AvatarService(
            IUserManager userManager,
            IProviderManager providerManager,
            IApplicationPaths appPaths,
            ILogger<AvatarService> logger)
        {
            _userManager = userManager;
            _providerManager = providerManager;
            _appPaths = appPaths;
            _logger = logger;

            _logger.LogInformation(
                "AvatarService constructor called. Plugin.Instance is {Status}",
                Plugin.Instance == null ? "NULL" : "initialized");

            // Store avatars in plugin data directory using Jellyfin's proper paths
            // This path is automatically adapted to the environment (Docker, Windows, Linux, etc.)
            var pluginDataPath = Path.Combine(
                _appPaths.PluginConfigurationsPath,
                "GetAvatar",
                "avatars");

            _avatarDirectory = pluginDataPath;

            _logger.LogInformation("Avatar directory path: {Path}", _avatarDirectory);

            // Create directory if it doesn't exist
            try
            {
                if (!Directory.Exists(_avatarDirectory))
                {
                    Directory.CreateDirectory(_avatarDirectory);
                    _logger.LogInformation("Successfully created avatar directory: {Path}", _avatarDirectory);
                }
                else
                {
                    _logger.LogInformation("Avatar directory already exists: {Path}", _avatarDirectory);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission denied when creating avatar directory at {Path}. Please check directory permissions.", _avatarDirectory);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create avatar directory at {Path}", _avatarDirectory);
                throw;
            }
        }

        /// <summary>
        /// Gets the avatar directory path.
        /// </summary>
        public string AvatarDirectory => _avatarDirectory;

        /// <summary>
        /// Gets all available avatars from configuration.
        /// </summary>
        /// <returns>List of available avatars.</returns>
        public List<AvatarInfo> GetAvailableAvatars()
        {
            if (Plugin.Instance == null)
            {
                _logger.LogError("Plugin instance is null in GetAvailableAvatars");
                return new List<AvatarInfo>();
            }

            var config = Plugin.Instance.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return new List<AvatarInfo>();
            }

            _logger.LogInformation("Returning {Count} avatars from configuration", config.AvailableAvatars?.Count ?? 0);
            return config.AvailableAvatars ?? new List<AvatarInfo>();
        }

        /// <summary>
        /// Saves an avatar image to the avatar directory.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="imageData">The image data.</param>
        /// <returns>The saved avatar info.</returns>
        public async Task<AvatarInfo> SaveAvatarAsync(string fileName, byte[] imageData)
        {
            try
            {
                var avatarId = Guid.NewGuid().ToString();
                var fileExtension = Path.GetExtension(fileName);
                var savedFileName = $"{avatarId}{fileExtension}";
                var filePath = Path.Combine(_avatarDirectory, savedFileName);

                await File.WriteAllBytesAsync(filePath, imageData);

                var avatarInfo = new AvatarInfo
                {
                    Id = avatarId,
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    FileName = savedFileName,
                    DateAdded = DateTime.UtcNow
                };

                // Add to configuration
                if (Plugin.Instance == null)
                {
                    _logger.LogError("Plugin instance is null, cannot save avatar to configuration");
                    throw new InvalidOperationException("Plugin not initialized");
                }

                var config = Plugin.Config;
                config.AvailableAvatars ??= new List<AvatarInfo>();
                config.AvailableAvatars.Add(avatarInfo);
                Plugin.Instance.SaveConfiguration();

                _logger.LogInformation("Saved avatar: {Name} ({Id})", avatarInfo.Name, avatarInfo.Id);

                return avatarInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save avatar: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Deletes an avatar.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <returns>True if deleted successfully.</returns>
        public bool DeleteAvatar(string avatarId)
        {
            try
            {
                if (Plugin.Instance == null)
                {
                    _logger.LogError("Plugin instance is null");
                    return false;
                }

                var config = Plugin.Config;
                var avatar = config.AvailableAvatars?.FirstOrDefault(a => a.Id == avatarId);

                if (avatar == null)
                {
                    _logger.LogWarning("Avatar not found: {Id}", avatarId);
                    return false;
                }

                var filePath = Path.Combine(_avatarDirectory, avatar.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                config.AvailableAvatars?.Remove(avatar);
                Plugin.Instance.SaveConfiguration();

                _logger.LogInformation("Deleted avatar: {Name} ({Id})", avatar.Name, avatarId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete avatar: {Id}", avatarId);
                return false;
            }
        }

        /// <summary>
        /// Gets the path to an avatar image.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <returns>The file path, or null if not found.</returns>
        public string? GetAvatarPath(string avatarId)
        {
            if (Plugin.Instance == null)
            {
                _logger.LogError("Plugin instance is null");
                return null;
            }

            var avatar = Plugin.Config.AvailableAvatars?.FirstOrDefault(a => a.Id == avatarId);
            if (avatar == null)
            {
                return null;
            }

            var filePath = Path.Combine(_avatarDirectory, avatar.FileName);
            return File.Exists(filePath) ? filePath : null;
        }

        /// <summary>
        /// Sets an avatar for a user using Jellyfin's internal API.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="authToken">The authentication token (not used).</param>
        /// <returns>Task representing the operation.</returns>
        public async Task SetUserAvatarAsync(Guid userId, string avatarId, string? authToken = null)
        {
            _logger.LogInformation("SetUserAvatarAsync called: userId={UserId}, avatarId={AvatarId}", userId, avatarId);

            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                _logger.LogError("User not found: {UserId}", userId);
                throw new ArgumentException($"User not found: {userId}");
            }

            _logger.LogInformation("Found user: {UserName}", user.Username);

            var avatarPath = GetAvatarPath(avatarId);
            if (avatarPath == null)
            {
                _logger.LogError("Avatar not found: {AvatarId}", avatarId);
                throw new ArgumentException($"Avatar not found: {avatarId}");
            }

            _logger.LogInformation("Avatar path: {Path}", avatarPath);

            // Determine file extension and MIME type
            var extension = Path.GetExtension(avatarPath).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            _logger.LogInformation("MIME type: {MimeType}, Extension: {Extension}", mimeType, extension);

            // Build the user's profile image path with unique timestamp
            var userDataPath = Path.Combine(_appPaths.DataPath, "users", userId.ToString("N"));
            _logger.LogInformation("User data path: {Path}", userDataPath);

            Directory.CreateDirectory(userDataPath);

            // Use timestamp to ensure unique filename (forces cache refresh)
            var timestamp = DateTime.UtcNow.Ticks;
            var profileImagePath = Path.Combine(userDataPath, $"profile_{timestamp}{extension}");
            _logger.LogInformation("Profile image path: {Path}", profileImagePath);

            try
            {
                // Remember the old path before making changes
                string? oldProfileImagePath = null;
                if (user.ProfileImage != null && !string.IsNullOrEmpty(user.ProfileImage.Path))
                {
                    oldProfileImagePath = user.ProfileImage.Path;
                }

                // Copy the new avatar file first (before deleting anything)
                File.Copy(avatarPath, profileImagePath, overwrite: true);
                _logger.LogInformation("Copied avatar to profile path");

                // Update or create user's profile image reference
                if (user.ProfileImage != null)
                {
                    user.ProfileImage.Path = profileImagePath;
                    _logger.LogInformation("Updated existing profile image path");
                }
                else
                {
                    user.ProfileImage = new ImageInfo(profileImagePath);
                    _logger.LogInformation("Created new profile image reference");
                }

                await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
                _logger.LogInformation("Saved user changes to database");

                // Delete old profile image file after the database update succeeded
                if (oldProfileImagePath != null
                    && oldProfileImagePath != profileImagePath
                    && File.Exists(oldProfileImagePath))
                {
                    try
                    {
                        File.Delete(oldProfileImagePath);
                        _logger.LogInformation("Deleted old profile image: {Path}", oldProfileImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete old profile image (orphan file left)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save/update profile image");
                throw;
            }

            // Store the mapping in plugin config
            if (Plugin.Instance == null)
            {
                _logger.LogError("Plugin instance is null, cannot save avatar mapping");
                throw new InvalidOperationException("Plugin not initialized");
            }

            var config = Plugin.Config;
            config.UserAvatars ??= new List<UserAvatarMapping>();

            var existingMapping = config.UserAvatars.FirstOrDefault(x => x.UserId == userId.ToString());
            if (existingMapping != null)
            {
                existingMapping.AvatarId = avatarId;
            }
            else
            {
                config.UserAvatars.Add(new UserAvatarMapping
                {
                    UserId = userId.ToString(),
                    AvatarId = avatarId
                });
            }

            Plugin.Instance.SaveConfiguration();

            _logger.LogInformation("Successfully set avatar {AvatarId} for user {UserName} ({UserId})", avatarId, user.Username, userId);
        }

        /// <summary>
        /// Gets the avatar ID that a user has selected.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The avatar ID, or null if no avatar is set.</returns>
        public string? GetUserAvatarId(Guid userId)
        {
            var config = Plugin.Config;
            if (config.UserAvatars == null)
            {
                return null;
            }

            var mapping = config.UserAvatars.FirstOrDefault(x => x.UserId == userId.ToString());
            return mapping?.AvatarId;
        }
    }
}
