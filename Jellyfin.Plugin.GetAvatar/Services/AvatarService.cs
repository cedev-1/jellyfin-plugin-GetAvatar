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
        /// Deletes an avatar from the pool.
        /// Note: This does NOT remove the avatar from users who are currently using it.
        /// The user's profile image remains intact until they select a different avatar.
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

                // Check if any user is currently using this avatar
                var usersWithAvatar = config.UserAvatars?.Where(u => u.AvatarId == avatarId).ToList() ?? new List<UserAvatarMapping>();
                if (usersWithAvatar.Any())
                {
                    _logger.LogWarning(
                        "Avatar {AvatarId} is currently used by {UserCount} user(s). " +
                        "Removing from pool but keeping user profile images intact.",
                        avatarId,
                        usersWithAvatar.Count);

                    // Remove the mapping from config (users will keep their current profile image)
                    // This decouples the pool avatar from the user assignment
                    config.UserAvatars?.RemoveAll(u => u.AvatarId == avatarId);
                }

                // Delete the avatar file from the pool
                var filePath = Path.Combine(_avatarDirectory, avatar.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted avatar file: {FilePath}", filePath);
                }

                // Remove from available avatars list
                config.AvailableAvatars?.Remove(avatar);
                Plugin.Instance.SaveConfiguration();

                _logger.LogInformation("Deleted avatar from pool: {Name} ({Id})", avatar.Name, avatarId);
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

            // Build the user's profile image path
            var userDataPath = Path.Combine(_appPaths.DataPath, "users", userId.ToString("N"));
            _logger.LogInformation("User data path: {Path}", userDataPath);

            Directory.CreateDirectory(userDataPath);

            // Use avatarId in filename for stability (same avatar = same filename pattern)
            // Add timestamp suffix to force cache refresh
            var timestamp = DateTime.UtcNow.Ticks;
            var profileImagePath = Path.Combine(userDataPath, $"profile_avatar_{avatarId}_{timestamp}{extension}");
            _logger.LogInformation("Profile image path: {Path}", profileImagePath);

            string? oldProfileImagePath = null;
            var fileCopied = false;

            try
            {
                // Remember the old path before making changes
                if (user.ProfileImage != null && !string.IsNullOrEmpty(user.ProfileImage.Path))
                {
                    oldProfileImagePath = user.ProfileImage.Path;
                }

                // Clean up old profile files first to prevent accumulation
                await CleanupOldProfileFilesAsync(userDataPath, oldProfileImagePath).ConfigureAwait(false);

                // Copy the new avatar file first (before deleting anything)
                File.Copy(avatarPath, profileImagePath, overwrite: true);
                fileCopied = true;
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

                // Clean up the copied file if database update failed
                if (fileCopied && File.Exists(profileImagePath))
                {
                    try
                    {
                        File.Delete(profileImagePath);
                        _logger.LogInformation("Cleaned up orphaned file after failed update: {Path}", profileImagePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Could not clean up orphaned file: {Path}", profileImagePath);
                    }
                }

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
        /// Cleans up old profile image files in the user directory, keeping only the current one.
        /// </summary>
        private async Task CleanupOldProfileFilesAsync(string userDataPath, string? currentProfilePath)
        {
            try
            {
                if (!Directory.Exists(userDataPath))
                {
                    return;
                }

                var profileFiles = Directory.GetFiles(userDataPath, "profile_*");
                foreach (var file in profileFiles)
                {
                    if (string.Equals(file, currentProfilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                using var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                            }
                            catch (IOException)
                            {
                                return;
                            }

                            File.Delete(file);
                        }).ConfigureAwait(false);

                        _logger.LogDebug("Cleaned up old profile file: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not delete old profile file (may be in use): {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during old profile file cleanup");
            }
        }

        /// <summary>
        /// Gets the avatar ID that a user has selected.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The avatar ID, or null if no avatar is set.</returns>
        public string? GetUserAvatarId(Guid userId)
        {
            if (Plugin.Instance == null)
            {
                _logger.LogError("Plugin instance is null in GetUserAvatarId");
                return null;
            }

            var config = Plugin.Config;
            if (config.UserAvatars == null)
            {
                return null;
            }

            var mapping = config.UserAvatars.FirstOrDefault(x => x.UserId == userId.ToString());
            return mapping?.AvatarId;
        }

        /// <summary>
        /// Validates all user avatars and repairs any missing profile images.
        /// This should be called at plugin startup to ensure avatars are not lost.
        /// </summary>
        /// <returns>The number of avatars that were repaired.</returns>
        public async Task<int> ValidateUserAvatarsAsync()
        {
            if (Plugin.Instance == null)
            {
                _logger.LogError("Plugin instance is null, cannot validate avatars");
                return 0;
            }

            var config = Plugin.Config;
            if (config.UserAvatars == null || !config.UserAvatars.Any())
            {
                _logger.LogInformation("No user avatars to validate");
                return 0;
            }

            var repairedCount = 0;
            var mappingsToRemove = new List<UserAvatarMapping>();

            foreach (var mapping in config.UserAvatars.ToList())
            {
                try
                {
                    if (!Guid.TryParse(mapping.UserId, out var userId))
                    {
                        _logger.LogWarning("Invalid user ID in mapping: {UserId}", mapping.UserId);
                        mappingsToRemove.Add(mapping);
                        continue;
                    }

                    var user = _userManager.GetUserById(userId);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found for avatar mapping: {UserId}", mapping.UserId);
                        mappingsToRemove.Add(mapping);
                        continue;
                    }

                    var profileImageExists = user.ProfileImage != null
                        && !string.IsNullOrEmpty(user.ProfileImage.Path)
                        && File.Exists(user.ProfileImage.Path);

                    if (profileImageExists)
                    {
                        _logger.LogDebug("Avatar for user {UserName} is valid", user.Username);
                        continue;
                    }

                    _logger.LogWarning(
                        "Profile image missing for user {UserName} (expected: {Path}). Attempting to repair...",
                        user.Username,
                        user.ProfileImage?.Path ?? "null");

                    var avatarPath = GetAvatarPath(mapping.AvatarId);
                    if (avatarPath == null)
                    {
                        _logger.LogError(
                            "Cannot repair avatar for user {UserName}: Avatar {AvatarId} no longer exists in pool",
                            user.Username,
                            mapping.AvatarId);

                        mappingsToRemove.Add(mapping);

                        if (user.ProfileImage != null)
                        {
                            user.ProfileImage = null;
                            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
                            _logger.LogInformation("Cleared profile image reference for user {UserName}", user.Username);
                        }

                        continue;
                    }

                    await SetUserAvatarAsync(userId, mapping.AvatarId);
                    repairedCount++;
                    _logger.LogInformation("Successfully repaired avatar for user {UserName}", user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating avatar for user {UserId}", mapping.UserId);
                }
            }

            foreach (var mapping in mappingsToRemove)
            {
                config.UserAvatars?.Remove(mapping);
            }

            if (mappingsToRemove.Any())
            {
                Plugin.Instance.SaveConfiguration();
                _logger.LogInformation("Removed {Count} invalid avatar mappings", mappingsToRemove.Count);
            }

            _logger.LogInformation(
                "Avatar validation complete. Repaired: {Repaired}, Removed invalid: {Removed}",
                repairedCount,
                mappingsToRemove.Count);

            return repairedCount;
        }

        /// <summary>
        /// Cleans up orphaned profile image files in user directories.
        /// This removes old profile images that are no longer referenced by the user.
        /// </summary>
        /// <returns>The number of orphaned files deleted.</returns>
        public int CleanOrphanedProfileImages()
        {
            var deletedCount = 0;

            try
            {
                var users = _userManager.Users;
                foreach (var user in users)
                {
                    try
                    {
                        var userDataPath = Path.Combine(_appPaths.DataPath, "users", user.Id.ToString("N"));
                        if (!Directory.Exists(userDataPath))
                        {
                            continue;
                        }

                        var profileFiles = Directory.GetFiles(userDataPath, "profile_*");
                        var currentProfilePath = user.ProfileImage?.Path;

                        foreach (var file in profileFiles)
                        {
                            if (string.Equals(file, currentProfilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                                _logger.LogDebug("Deleted orphaned profile image: {File}", file);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Could not delete orphaned file: {File}", file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning profile images for user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("Cleaned up {Count} orphaned profile images", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned profile image cleanup");
            }

            return deletedCount;
        }

        /// <summary>
        /// Removes the avatar assignment from a user without deleting the avatar from the pool.
        /// This clears the user's profile image and removes the mapping.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> RemoveUserAvatarAsync(Guid userId)
        {
            try
            {
                if (Plugin.Instance == null)
                {
                    _logger.LogError("Plugin instance is null");
                    return false;
                }

                var user = _userManager.GetUserById(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                var oldProfileImagePath = user.ProfileImage?.Path;

                user.ProfileImage = null;
                await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(oldProfileImagePath) && File.Exists(oldProfileImagePath))
                {
                    try
                    {
                        File.Delete(oldProfileImagePath);
                        _logger.LogInformation("Deleted profile image for user {UserName}", user.Username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete profile image file");
                    }
                }

                var config = Plugin.Config;
                var mapping = config.UserAvatars?.FirstOrDefault(x => x.UserId == userId.ToString());
                if (mapping != null)
                {
                    config.UserAvatars?.Remove(mapping);
                    Plugin.Instance.SaveConfiguration();
                }

                _logger.LogInformation("Removed avatar assignment for user {UserName}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove avatar for user {UserId}", userId);
                return false;
            }
        }
    }
}
