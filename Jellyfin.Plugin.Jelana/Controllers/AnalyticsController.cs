using Jellyfin.Plugin.Jelana.Models;
using Jellyfin.Plugin.Jelana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jelana.Controllers;

/// <summary>
/// Every endpoint requires a Jellyfin administrator (the built-in
/// <c>RequiresElevation</c> policy). Nothing here is anonymous and no
/// endpoint accepts credentials through a URL.
/// </summary>
[ApiController]
[Route("Jelana")]
[Authorize(Policy = "RequiresElevation")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly SnapshotStore _snapshots;
    public AnalyticsController(SnapshotStore snapshots) => _snapshots = snapshots;

    [HttpGet("Snapshot")]
    [ProducesResponseType(typeof(AnalyticsSnapshot), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsSnapshot>> Get(CancellationToken cancellationToken)
    {
        NoStore();
        var snapshot = await _snapshots.ReadAsync(cancellationToken).ConfigureAwait(false);
        return snapshot is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { Error = "Snapshot is being prepared." })
            : Ok(snapshot);
    }

    [HttpGet("Personal")]
    [ProducesResponseType(typeof(PersonalAnalytics), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonalAnalytics>> Personal(CancellationToken cancellationToken)
    {
        NoStore();
        var userId = User.FindFirst("Jellyfin-UserId")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();
        var personal = await _snapshots.ReadPersonalAsync(userId, cancellationToken).ConfigureAwait(false);
        return personal is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { Error = "Personal snapshot is being prepared." })
            : Ok(personal);
    }

    [HttpGet("Logo.png")]
    public IActionResult Logo()
    {
        Response.Headers.CacheControl = "private,max-age=86400";
        var name = $"{typeof(Plugin).Namespace}.Web.jelana-logo.png";
        var stream = typeof(Plugin).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing resource {name}.");
        return File(stream, "image/png");
    }

    private void NoStore()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }
}
