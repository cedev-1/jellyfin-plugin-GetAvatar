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
        private readonly IUserManager _userManager;
        private readonly ILogger<AvatarController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarController"/> class.
        /// </summary>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger instance.</param>
        public AvatarController(
            AvatarService avatarService,
            IUserManager userManager,
            ILogger<AvatarController> logger)
        {
            _avatarService = avatarService;
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
                var avatars = _avatarService.GetAvailableAvatars();
                return Ok(avatars.Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.FileName,
                    a.DateAdded,
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
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
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
        /// <returns>The created avatar info.</returns>
        [HttpPost("Upload")]
        [Authorize(Policy = "RequiresElevation")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
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

                var avatarInfo = await _avatarService.SaveAvatarAsync(file.FileName, imageData);

                return Ok(new
                {
                    avatarInfo.Id,
                    avatarInfo.Name,
                    avatarInfo.FileName,
                    avatarInfo.DateAdded,
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
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
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

                // Get current user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == "sub" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    // Try to get user from username claim
                    var usernameClaim = User.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
                    if (usernameClaim != null)
                    {
                        var user = _userManager.GetUserByName(usernameClaim.Value);
                        if (user != null)
                        {
                            userId = user.Id;
                        }
                        else
                        {
                            return Unauthorized("User not found");
                        }
                    }
                    else
                    {
                        return Unauthorized("User not authenticated");
                    }
                }

                // Extract the auth token from the request
                var authToken = Request.Headers["X-Emby-Token"].FirstOrDefault();
                if (string.IsNullOrEmpty(authToken))
                {
                    authToken = Request.Query["api_key"].FirstOrDefault();
                }

                await _avatarService.SetUserAvatarAsync(userId, request.AvatarId, authToken);

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
    }
}
