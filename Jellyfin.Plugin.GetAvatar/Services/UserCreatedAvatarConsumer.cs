using System;
using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar.Services
{
    /// <summary>
    /// Assigns a random avatar to users as soon as they are created.
    /// This complements the startup validation so newly created users do not
    /// have to wait for a server restart to receive an avatar.
    /// </summary>
    public class UserCreatedAvatarConsumer : IEventConsumer<UserCreatedEventArgs>
    {
        private readonly AvatarService _avatarService;
        private readonly ILogger<UserCreatedAvatarConsumer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCreatedAvatarConsumer"/> class.
        /// </summary>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="logger">The logger instance.</param>
        public UserCreatedAvatarConsumer(AvatarService avatarService, ILogger<UserCreatedAvatarConsumer> logger)
        {
            _avatarService = avatarService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task OnEvent(UserCreatedEventArgs eventArgs)
        {
            if (eventArgs?.Argument == null)
            {
                return;
            }

            var user = eventArgs.Argument;

            if (Plugin.Instance == null || !Plugin.Config.EnableAssignOnUserCreation)
            {
                return;
            }

            try
            {
                var assigned = await _avatarService
                    .AssignAvatarToUserIfMissingAsync(user.Id, force: true)
                    .ConfigureAwait(false);

                if (assigned)
                {
                    _logger.LogInformation("Auto-assigned a random avatar to newly created user {UserName}", user.Username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-assign avatar to newly created user {UserId}", user.Id);
            }
        }
    }
}
