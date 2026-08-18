# ForgeCare Sprint 14B — Remote Update Discovery

Version: **v0.0.35-beta**

## Added

- HTTPS-only remote `release-manifest.json` discovery.
- Configurable remote manifest URL.
- Configurable update channel (`beta` by default).
- 8-second network timeout.
- Clear states:
  - UPDATE AVAILABLE
  - CURRENT
  - OLDER BUILD
  - CHANNEL MISMATCH
  - HTTPS REQUIRED
  - OFFLINE / CHECK FAILED
  - CHECK TIMEOUT
  - INVALID MANIFEST
- Stable ForgeCare AppId validation before version comparison.
- Local persistence of:
  - manifest URL
  - update channel
  - last successful check
  - last known available version
  - last known state
- Existing Sprint 14A local-manifest checker remains available.

## Security boundary

Sprint 14B is discovery-only.

ForgeCare does **not**:
- download update artifacts,
- execute installers,
- silently update itself,
- bypass Windows security,
- accept plain HTTP update manifests.

## Future 14C

The next stage can use manifest artifact metadata to implement:

`explicit download → SHA-256 verification → technician approval`

without auto-installing anything.
