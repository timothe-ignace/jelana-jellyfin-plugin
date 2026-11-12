# Jelana for Jellyfin

A standalone, cache-first analytics plugin for Jellyfin.

## Jellyfin compatibility

| Jellyfin server | Jelana version | Download |
| --- | --- | --- |
| Jellyfin 12.0 or later | Jelana 0.2.x (admin-only fork) | Install the latest release from the repository catalog |
| Jellyfin 10.11.x | Jelana 0.1.25.0 | [Download Jelana 0.1.25.0 for Jellyfin 10.11](https://github.com/skijk/jellyfin-plugin-jelana-repository/releases/download/0.1.25.0/Jelana_0.1.25.0_jf10.11.11.zip) |

**Jelana 0.2.0.0 and later require Jellyfin 12 and cannot be installed on
Jellyfin 10.11.** The retained 0.1.25.0 release is the final version for
Jellyfin 10.11.x.

Jelana is independent from the standalone PHP application and uses Jellyfin's
Playback Reporting plugin as its history source. The UI only reads an atomically
replaced snapshot; it never runs analytics or Playback Reporting database
queries during a page request.

## Cache architecture

1. Jellyfin Playback Reporting records playback history.
2. Jellyfin's scheduled-task system reads that database in one bounded,
   read-only pass at startup and hourly.
3. A completed snapshot atomically replaces the previous file.
4. The authenticated API and the Statistics page only read that snapshot.

The last good snapshot remains available if a refresh fails.
The media codec and resolution profile has a separate six-hour cache, matching
the standalone Jelana implementation. Poster images use Jellyfin's own cached
image endpoint.

Playback counts use the same 30-minute session-gap CTE as Jelana. Playback
summaries, 7/30-day movie and series rankings, user rankings, daily activity,
playback methods and clients are calculated from Playback Reporting.

Trending is intentionally different from Most played. A person counts at most
once per title in each seven-day period, regardless of restarts, repeat views or
the number of series episodes watched. Short starts are ignored: a viewer must
watch 20 percent of an item's runtime, clamped to 5–20 minutes. When runtime is
unavailable, the fallback is 10 minutes for movies and 5 minutes for series.
Fragments are added together before the threshold is evaluated.

Titles need at least two qualified viewers. They are ranked by current unique
viewers, unique-viewer growth over the previous week, and the days on which
those viewers first watched. Most played rankings remain volume-based.

There is deliberately no HTTP endpoint that rebuilds statistics. Refreshes only
run through Jellyfin's scheduled-task system, ensuring that opening or reloading
the Statistics page cannot touch Playback Reporting or scan the media library.

## Dependencies

| Component | Status | Used for |
| --- | --- | --- |
| Jellyfin Server 12.0 | Required | Supported server and plugin ABI |
| [Playback Reporting](https://github.com/jellyfin/jellyfin-plugin-playbackreporting) | Required | Historical playback source read only by the scheduled cache job |
| [JellySpotlight](https://github.com/skijk/jellyfin-plugin-jellyspotlight) | Optional consumer | Can display Jelana's cached Trending and Popular new arrivals data |

Jelana does not require File Transformation, JellySpotlight, JellyBulletin,
Arr Watch or the old standalone PHP application.

## Installation

1. Install Playback Reporting from the official Jellyfin plugin catalog and
   allow it to begin recording playback.
2. In Jellyfin, add this plugin repository (Dashboard → Plugins → Repositories):

   ```text
   https://raw.githubusercontent.com/timothe-ignace/jelana-jellyfin-plugin/main/manifest.json
   ```

3. Install Jelana and restart Jellyfin. (Or build from source, see **Build**.)
4. Run **Dashboard → Scheduled Tasks → Jelana → Refresh Jelana analytics
   snapshot**, or wait for the startup/hourly refresh.

## Build

```bash
dotnet build Jellyfin.Plugin.Jelana.sln --configuration Release
```

Install the resulting DLL in a `Jelana` directory below Jellyfin's plugin
directory, then restart Jellyfin.

## Access control (this fork)

This fork is **administrator-only**.

- Every `/Jelana/*` endpoint uses Jellyfin's `RequiresElevation` policy:
  non-admin users, API keys without admin rights and anonymous requests get
  401/403. Nothing is `[AllowAnonymous]`.
- The standalone `/Jelana/User` page, the `Menu.js` loader and the JS Injector
  integration were removed. Analytics are shown only on the dashboard plugin
  page (**Dashboard → Analytics**), where Jellyfin's own web client supplies
  authentication.
- Removed token handling: no access token in URLs, `sessionStorage`, or
  `localStorage`, and no `api_key` query parameter.
- CSS and JS are inlined in the plugin page; API responses are `no-store`.
- Jellyfin itself serves the plugin page's static HTML shell without
  authentication (`/web/ConfigurationPage?name=Jelana`). It contains no data.
- Prebuilt binaries were removed from the repository: build from source.
- Versions here are based on upstream 0.2.7.0, which was verified to match the
  published release DLL.

See [SECURITY.md](SECURITY.md).
