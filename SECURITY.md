# Security

Security model of this fork of `skijk/jellyfin-plugin-jelana`:

- Admin-only: all `/Jelana/*` endpoints require Jellyfin's `RequiresElevation`.
- No outbound network access, no process execution, no dynamic code loading.
- Playback Reporting's database is opened read-only (`query_only`), with
  parameterized SQL; SQL fragments come from a fixed whitelist.
- The UI renders all data with `textContent` (no `innerHTML`).
- No tokens in URLs or browser storage.

Report issues through GitHub issues on this repository (or privately via a
security advisory).
