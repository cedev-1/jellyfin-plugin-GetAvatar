using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using Jellyfin.Plugin.GetAvatar.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar.Controllers
{
    /// <summary>
    /// API controller for managing avatars.
    /// </summary>
    [ApiController]
    [Route("GetAvatar")]
    [Authorize]
    public class AvatarController : ControllerBase
    {
        private readonly AvatarService _avatarService;
        private readonly OnlinePackService _onlinePackService;
        private readonly IUserManager _userManager;
        private readonly ILogger<AvatarController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarController"/> class.
        /// </summary>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="onlinePackService">The online pack service.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger instance.</param>
        public AvatarController(
            AvatarService avatarService,
            OnlinePackService onlinePackService,
            IUserManager userManager,
            ILogger<AvatarController> logger)
        {
            _avatarService = avatarService;
            _onlinePackService = onlinePackService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets the client-side JavaScript for avatar selection.
        /// </summary>
        /// <returns>The JavaScript file.</returns>
        [HttpGet("ClientScript")]
        [AllowAnonymous]
        public IActionResult GetClientScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.GetAvatar.Configuration.Web.clientScript.js";

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogError("Client script resource not found: {ResourceName}", resourceName);
                return NotFound();
            }

            return File(stream, "application/javascript");
        }

        /// <summary>
        /// Gets all available avatars.
        /// </summary>
        /// <returns>List of available avatars.</returns>
        [HttpGet("Avatars")]
        public ActionResult<IEnumerable<object>> GetAvatars()
        {
            try
            {
                _logger.LogInformation("GetAvatars endpoint called");
                var avatars = _avatarService.GetAvailableAvatars();
                _logger.LogInformation("Retrieved {Count} avatars", avatars.Count);
                return Ok(avatars.Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.FileName,
                    a.DateAdded,
                    a.Category,
                    Url = $"/GetAvatar/Image/{a.Id}"
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get avatars");
                return StatusCode(500, "Failed to retrieve avatars");
            }
        }

        /// <summary>
        /// Gets an avatar image.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <returns>The avatar image.</returns>
        [HttpGet("Image/{avatarId}")]
        [AllowAnonymous]
        public IActionResult GetAvatarImage(string avatarId)
        {
            try
            {
                var avatarPath = _avatarService.GetAvatarPath(avatarId);
                if (avatarPath == null || !System.IO.File.Exists(avatarPath))
                {
                    return NotFound();
                }

                var extension = Path.GetExtension(avatarPath).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                var image = System.IO.File.OpenRead(avatarPath);
                return File(image, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get avatar image: {AvatarId}", avatarId);
                return StatusCode(500, "Failed to retrieve avatar image");
            }
        }

        /// <summary>
        /// Uploads a new avatar (admin only).
        /// </summary>
        /// <param name="file">The image file.</param>
        /// <param name="category">The optional category for the avatar.</param>
        /// <returns>The created avatar info.</returns>
        [HttpPost("Upload")]
        [Authorize(Policy = "RequiresElevation")]
        public async Task<IActionResult> UploadAvatar(IFormFile file, [FromQuery] string category = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Invalid file type. Only images are allowed.");
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds 5MB limit");
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                var avatarInfo = await _avatarService.SaveAvatarAsync(file.FileName, imageData, category);

                return Ok(new
                {
                    avatarInfo.Id,
                    avatarInfo.Name,
                    avatarInfo.FileName,
                    avatarInfo.DateAdded,
                    avatarInfo.Category,
                    Url = $"/GetAvatar/Image/{avatarInfo.Id}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload avatar");
                return StatusCode(500, "Failed to upload avatar");
            }
        }

        /// <summary>
        /// Deletes an avatar (admin only).
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <returns>Status of deletion.</returns>
        [HttpDelete("Delete/{avatarId}")]
        [Authorize(Policy = "RequiresElevation")]
        public IActionResult DeleteAvatar(string avatarId)
        {
            try
            {
                var success = _avatarService.DeleteAvatar(avatarId);
                if (!success)
                {
                    return NotFound();
                }

                return Ok(new { message = "Avatar deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete avatar: {AvatarId}", avatarId);
                return StatusCode(500, "Failed to delete avatar");
            }
        }

        /// <summary>
        /// Gets the custom avatar for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user's custom avatar image.</returns>
        [HttpGet("UserAvatar/{userId}")]
        [AllowAnonymous]
        public IActionResult GetUserAvatar(string userId)
        {
            try
            {
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return BadRequest("Invalid user ID");
                }

                var avatarId = _avatarService.GetUserAvatarId(userGuid);
                if (string.IsNullOrEmpty(avatarId))
                {
                    return NotFound(new { message = "No custom avatar set for this user" });
                }

                var avatarPath = _avatarService.GetAvatarPath(avatarId);
                if (avatarPath == null || !System.IO.File.Exists(avatarPath))
                {
                    return NotFound(new { message = "Avatar file not found" });
                }

                var extension = Path.GetExtension(avatarPath).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                var image = System.IO.File.OpenRead(avatarPath);
                return File(image, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user avatar: {UserId}", userId);
                return StatusCode(500, "Failed to retrieve user avatar");
            }
        }

        /// <summary>
        /// Sets an avatar for the current user.
        /// </summary>
        /// <param name="request">The request containing the avatar ID.</param>
        /// <returns>Status of operation.</returns>
        [HttpPost("SetAvatar")]
        public async Task<IActionResult> SetUserAvatar([FromBody] SetAvatarRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AvatarId))
                {
                    return BadRequest("Avatar ID is required");
                }

                // Get user ID
                Guid currentUserId = Guid.Empty;
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == "sub" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var claimUserId))
                {
                    currentUserId = claimUserId;
                }
                else
                {
                    var usernameClaim = User.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
                    if (usernameClaim != null)
                    {
                        var user = _userManager.GetUserByName(usernameClaim.Value);
                        if (user != null)
                        {
                            currentUserId = user.Id;
                        }
                    }
                }

                if (currentUserId == Guid.Empty)
                {
                    return Unauthorized("User not authenticated");
                }

                Guid targetUserId = Guid.Empty;

                if (!string.IsNullOrEmpty(request.UserId))
                {
                    if (Guid.TryParse(request.UserId, out var parsedUserId))
                    {
                        targetUserId = parsedUserId;
                    }
                    else
                    {
                        return BadRequest("Invalid user ID format");
                    }
                }
                else
                {
                    targetUserId = currentUserId;
                }

                if (targetUserId != currentUserId)
                {
                    if (!User.IsInRole("Administrator"))
                    {
                        return Forbid("Only administrators can modify other users' avatars");
                    }
                }

                await _avatarService.SetUserAvatarAsync(targetUserId, request.AvatarId);

                return Ok(new { message = "Avatar set successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set user avatar");
                return StatusCode(500, "Failed to set avatar");
            }
        }

        /// <summary>
        /// Removes the avatar from the current user (admin only).
        /// This clears the user's profile image without deleting the avatar from the pool.
        /// </summary>
        /// <param name="request">The request containing the user ID.</param>
        /// <returns>Status of operation.</returns>
        [HttpPost("RemoveUserAvatar")]
        [Authorize(Policy = "RequiresElevation")]
        public async Task<IActionResult> RemoveUserAvatar([FromBody] RemoveAvatarRequest request)
        {
            try
            {
                if (!Guid.TryParse(request.UserId, out var userId))
                {
                    return BadRequest("Invalid user ID");
                }

                var success = await _avatarService.RemoveUserAvatarAsync(userId);
                if (!success)
                {
                    return BadRequest("Failed to remove avatar from user");
                }

                return Ok(new { message = "Avatar removed from user successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user avatar");
                return StatusCode(500, "Failed to remove avatar");
            }
        }

        /// <summary>
        /// Validates and repairs all user avatars (admin only).
        /// This checks if profile image files exist and re-applies avatars if missing.
        /// </summary>
        /// <returns>Validation results.</returns>
        [HttpPost("ValidateAvatars")]
        [Authorize(Policy = "RequiresElevation")]
        public async Task<IActionResult> ValidateAvatars()
        {
            try
            {
                _logger.LogInformation("Starting avatar validation (manual trigger)");
                var repairedCount = await _avatarService.ValidateUserAvatarsAsync();

                return Ok(new
                {
                    message = "Avatar validation completed",
                    repairedCount = repairedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate avatars");
                return StatusCode(500, "Failed to validate avatars");
            }
        }

        /// <summary>
        /// Cleans up orphaned profile image files (admin only).
        /// This removes old profile images that are no longer referenced.
        /// </summary>
        /// <returns>Cleanup results.</returns>
        [HttpPost("CleanupOrphans")]
        [Authorize(Policy = "RequiresElevation")]
        public IActionResult CleanupOrphanedFiles()
        {
            try
            {
                _logger.LogInformation("Starting orphaned file cleanup (manual trigger)");
                var deletedCount = _avatarService.CleanOrphanedProfileImages();

                return Ok(new
                {
                    message = "Orphaned file cleanup completed",
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup orphaned files");
                return StatusCode(500, "Failed to cleanup orphaned files");
            }
        }

        /// <summary>
        /// Gets the status of user avatars (admin only).
        /// </summary>
        /// <returns>Status information for all users with avatars.</returns>
        [HttpGet("AvatarStatus")]
        [Authorize(Policy = "RequiresElevation")]
        public IActionResult GetAvatarStatus()
        {
            try
            {
                var userStatuses = GetAllUsers().Select(user =>
                {
                    var avatarId = _avatarService.GetUserAvatarId(user.Id);
                    var profileImageExists = user.ProfileImage != null
                        && !string.IsNullOrEmpty(user.ProfileImage.Path)
                        && System.IO.File.Exists(user.ProfileImage.Path);
                    var status = avatarId != null && profileImageExists ? "ok" :
                                 avatarId != null ? "missing_file" :
                                 "no_avatar";
                    return new
                    {
                        userId = user.Id.ToString(),
                        username = user.Username,
                        avatarId,
                        hasProfileImage = user.ProfileImage != null,
                        profileImagePath = user.ProfileImage?.Path,
                        profileImageExists,
                        status
                    };
                }).ToList();

                return Ok(new
                {
                    totalUsers = userStatuses.Count,
                    usersWithAvatars = userStatuses.Count(u => u.avatarId != null),
                    usersWithMissingFiles = userStatuses.Count(u => u.status == "missing_file"),
                    users = userStatuses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get avatar status");
                return StatusCode(500, "Failed to get avatar status");
            }
        }

        /// <summary>
        /// Gets plugin feature settings.
        /// </summary>
        /// <returns>Current feature settings.</returns>
        [HttpGet("Settings")]
        [Authorize(Policy = "RequiresElevation")]
        public IActionResult GetSettings()
        {
            return Ok(new { enableAutoAssign = Plugin.Config.EnableAutoAssign });
        }

        /// <summary>
        /// Updates plugin feature settings.
        /// </summary>
        /// <param name="request">The settings to update.</param>
        /// <returns>Status of operation.</returns>
        [HttpPost("Settings")]
        [Authorize(Policy = "RequiresElevation")]
        public IActionResult UpdateSettings([FromBody] PluginSettingsRequest request)
        {
            if (Plugin.Instance == null)
            {
                return StatusCode(500, "Plugin not initialized");
            }

            var config = Plugin.Config;
            config.EnableAutoAssign = request.EnableAutoAssign;
            Plugin.Instance.SaveConfiguration();

            return Ok(new { message = "Settings saved" });
        }

        /// <summary>
        /// Gets the available online avatar packs from the latest GitHub release.
        /// </summary>
        /// <returns>List of available packs.</returns>
        [HttpGet("OnlinePacks")]
        [Authorize(Policy = "RequiresElevation")]
        public async Task<IActionResult> GetOnlinePacks()
        {
            try
            {
                var packs = await _onlinePackService.GetAvailablePacksAsync().ConfigureAwait(false);
                return Ok(packs.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    fileName = p.FileName,
                    downloadUrl = p.DownloadUrl,
                    size = p.Size
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get online avatar packs");
                return StatusCode(500, "Failed to retrieve online avatar packs");
            }
        }

        /// <summary>
        /// Imports the selected online avatar packs.
        /// </summary>
        /// <param name="request">The request containing the pack identifiers.</param>
        /// <returns>Import result.</returns>
        [HttpPost("ImportOnlinePacks")]
        [Authorize(Policy = "RequiresElevation")]
        public async Task<IActionResult> ImportOnlinePacks([FromBody] ImportOnlinePacksRequest request)
        {
            try
            {
                if (request.PackIds == null || request.PackIds.Count == 0)
                {
                    return BadRequest("No packs selected");
                }

                var result = await _onlinePackService.ImportPacksAsync(request.PackIds).ConfigureAwait(false);
                return Ok(new
                {
                    importedCount = result.ImportedCount,
                    totalImages = result.TotalImages,
                    duplicateCount = result.DuplicateCount,
                    packResults = result.PackResults.Select(r => new
                    {
                        r.PackId,
                        r.PackName,
                        r.ImportedCount,
                        r.Success,
                        r.ErrorMessage
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import online avatar packs");
                return StatusCode(500, "Failed to import online avatar packs");
            }
        }

        /// <summary>
        /// Gets all users, compatible with both Jellyfin ≤10.11.8 (Users property)
        /// and Jellyfin ≥10.11.9 (GetUsers() method).
        /// See the changelog for release 10.11.9: https://github.com/jellyfin/jellyfin/compare/v10.11.8...v10.11.9.
        /// </summary>
        private IEnumerable<Jellyfin.Database.Implementations.Entities.User> GetAllUsers()
        {
            var managerObj = (object)_userManager;
            var getUsersMethod = managerObj.GetType().GetMethod("GetUsers", Type.EmptyTypes);
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
    }
}
