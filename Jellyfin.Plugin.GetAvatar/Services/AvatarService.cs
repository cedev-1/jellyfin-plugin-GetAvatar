using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.GetAvatar.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
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
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly string _avatarDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        public AvatarService(
            IUserManager userManager,
            IProviderManager providerManager,
            IApplicationPaths appPaths,
            ILogger<AvatarService> logger,
            IServerConfigurationManager serverConfigurationManager)
        {
            _userManager = userManager;
            _providerManager = providerManager;
            _appPaths = appPaths;
            _logger = logger;
            _serverConfigurationManager = serverConfigurationManager;

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
        /// <param name="category">The optional category for the avatar.</param>
        /// <returns>The saved avatar info.</returns>
        public async Task<AvatarInfo> SaveAvatarAsync(string fileName, byte[] imageData, string category = "")
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
                    DateAdded = DateTime.UtcNow,
                    Category = category
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
        /// <returns>Task representing the operation.</returns>
        public async Task SetUserAvatarAsync(Guid userId, string avatarId)
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
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            _logger.LogInformation("MIME type: {MimeType}, Extension: {Extension}", mimeType, extension);

            // Use Jellyfin's native user profile image path and save pipeline.
            // This mirrors Jellyfin.Api.Controllers.ImageController.PostUserImage.
            var userDataPath = Path.Combine(
                _serverConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath,
                user.Username);

            _logger.LogInformation("User profile image directory: {Path}", userDataPath);

            Directory.CreateDirectory(userDataPath);

            var profileImagePath = Path.Combine(userDataPath, "profile" + extension);
            _logger.LogInformation("Profile image path: {Path}", profileImagePath);

            try
            {
                if (user.ProfileImage is not null)
                {
                    await _userManager.ClearProfileImageAsync(user).ConfigureAwait(false);
                    _logger.LogInformation("Cleared existing profile image for user {UserName}", user.Username);
                }

                user.ProfileImage = new ImageInfo(profileImagePath);

                await using var stream = File.OpenRead(avatarPath);
                await _providerManager
                    .SaveImage(stream, mimeType, profileImagePath)
                    .ConfigureAwait(false);

                await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

                _logger.LogInformation("Updated profile image via Jellyfin image pipeline: {Path}", profileImagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save/update profile image via Jellyfin image pipeline");

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
        /// Gets all users, compatible with both Jellyfin ≤10.11.8 (Users property)
        /// and Jellyfin ≥10.11.9 (GetUsers() method).
        /// See the changelog for release 10.11.9: https://github.com/jellyfin/jellyfin/compare/v10.11.8...v10.11.9.
        /// </summary>
        private IEnumerable<Jellyfin.Database.Implementations.Entities.User> GetAllUsers()
        {
            var managerObj = (object)_userManager;
            var getUsersMethod = managerObj.GetType().GetMethod("GetUsers", System.Type.EmptyTypes);
            if (getUsersMethod != null)
            {
                return (IEnumerable<Jellyfin.Database.Implementations.Entities.User>)(getUsersMethod.Invoke(_userManager, null) ?? Enumerable.Empty<Jellyfin.Database.Implementations.Entities.User>());
            }

            var usersProperty = managerObj.GetType().GetProperty("Users");
            if (usersProperty != null)
            {
                return (IEnumerable<Jellyfin.Database.Implementations.Entities.User>)(usersProperty.GetValue(_userManager) ?? Enumerable.Empty<Jellyfin.Database.Implementations.Entities.User>());
            }

            _logger.LogWarning("Could not find Users property or GetUsers() method on IUserManager");
            return Enumerable.Empty<Jellyfin.Database.Implementations.Entities.User>();
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
                var users = GetAllUsers();
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
        /// Gets a random avatar from the available pool.
        /// </summary>
        /// <returns>A random avatar info, or null if no avatars exist.</returns>
        public AvatarInfo? GetRandomAvatar()
        {
            var avatars = Plugin.Config.AvailableAvatars;
            if (avatars == null || avatars.Count == 0)
            {
                return null;
            }

            var index = new Random().Next(avatars.Count);
            return avatars[index];
        }

        /// <summary>
        /// Assigns a default or random avatar to all users who have no avatar set.
        /// </summary>
        /// <returns>The number of users who received an avatar.</returns>
        public async Task<int> AssignMissingAvatarsAsync()
        {
            if (Plugin.Instance == null)
            {
                return 0;
            }

            var avatars = Plugin.Config.AvailableAvatars;
            if (avatars == null || avatars.Count == 0)
            {
                return 0;
            }

            var config = Plugin.Config;

            if (!config.EnableAutoAssign)
            {
                return 0;
            }

            var assignedCount = 0;
            var users = GetAllUsers();

            foreach (var user in users)
            {
                try
                {
                    var hasProfileImage = user.ProfileImage != null
                        && !string.IsNullOrEmpty(user.ProfileImage.Path)
                        && File.Exists(user.ProfileImage.Path);

                    if (hasProfileImage)
                    {
                        continue;
                    }

                    var target = GetRandomAvatar();
                    if (target == null)
                    {
                        continue;
                    }

                    config.UserAvatars?.RemoveAll(x => x.UserId == user.Id.ToString());

                    await SetUserAvatarAsync(user.Id, target.Id).ConfigureAwait(false);
                    assignedCount++;
                    _logger.LogInformation("Auto-assigned random avatar {AvatarId} to user {UserName}", target.Id, user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-assign avatar to user {UserId}", user.Id);
                }
            }

            return assignedCount;
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
