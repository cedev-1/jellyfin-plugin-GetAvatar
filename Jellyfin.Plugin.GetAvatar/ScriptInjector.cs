using System.Text.RegularExpressions;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GetAvatar;

/// <summary>
/// Background service that injects the GetAvatar JavaScript into Jellyfin's index.html at server startup.
/// </summary>
public class ScriptInjector : IHostedService, IDisposable
{
    private const string StartComment = "<!-- GetAvatar Plugin Start -->";
    private const string EndComment = "<!-- GetAvatar Plugin End -->";

    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<ScriptInjector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptInjector"/> class.
    /// </summary>
    /// <param name="appPaths">Application paths.</param>
    /// <param name="logger">Logger instance.</param>
    public ScriptInjector(IApplicationPaths appPaths, ILogger<ScriptInjector> logger)
    {
        _appPaths = appPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            InjectScript();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject GetAvatar script");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Injects the script into index.html.
    /// </summary>
    private void InjectScript()
    {
        var indexPath = Path.Combine(_appPaths.WebPath, "index.html");

        if (!File.Exists(indexPath))
        {
            _logger.LogWarning("index.html not found at {Path}", indexPath);
            return;
        }

        var content = File.ReadAllText(indexPath);

        // Build injection block
        var injectionBlock = BuildInjectionBlock();

        // Check if already injected with same content
        if (content.Contains(injectionBlock))
        {
            _logger.LogDebug("GetAvatar script already injected");
            return;
        }

        // Remove any existing injection (for upgrades)
        var regex = new Regex($"{Regex.Escape(StartComment)}[\\s\\S]*?{Regex.Escape(EndComment)}", RegexOptions.Multiline);
        content = regex.Replace(content, string.Empty);

        // Find closing body tag and insert before it
        const string closingBodyTag = "</body>";
        var bodyIndex = content.LastIndexOf(closingBodyTag, StringComparison.OrdinalIgnoreCase);

        if (bodyIndex == -1)
        {
            _logger.LogWarning("Could not find </body> tag in index.html");
            return;
        }

        content = content.Insert(bodyIndex, injectionBlock + "\n");

        File.WriteAllText(indexPath, content);
        _logger.LogInformation("GetAvatar script injected successfully");
    }

    private static string BuildInjectionBlock()
    {
        return $@"{StartComment}
<script src=""/GetAvatar/ClientScript""></script>
{EndComment}";
    }
}
