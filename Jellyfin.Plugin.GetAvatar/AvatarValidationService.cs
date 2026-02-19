using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.GetAvatar.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar
{
    /// <summary>
    /// Hosted service that validates user avatars at startup.
    /// This ensures that profile images are repaired if they were deleted or lost.
    /// </summary>
    public class AvatarValidationService : IHostedService
    {
        private readonly AvatarService _avatarService;
        private readonly ILogger<AvatarValidationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarValidationService"/> class.
        /// </summary>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="logger">The logger instance.</param>
        public AvatarValidationService(AvatarService avatarService, ILogger<AvatarValidationService> logger)
        {
            _avatarService = avatarService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetAvatar validation service starting...");

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                var repairedCount = await _avatarService.ValidateUserAvatarsAsync().ConfigureAwait(false);

                if (repairedCount > 0)
                {
                    _logger.LogInformation("Avatar validation completed. Repaired {Count} missing avatar(s).", repairedCount);
                }
                else
                {
                    _logger.LogInformation("Avatar validation completed. All avatars are valid.");
                }

                var deletedCount = _avatarService.CleanOrphanedProfileImages();
                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} orphaned profile image(s).", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during avatar validation at startup");
            }
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetAvatar validation service stopping...");
            return Task.CompletedTask;
        }
    }
}
