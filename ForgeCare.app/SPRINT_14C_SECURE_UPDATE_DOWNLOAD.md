# ForgeCare Sprint 14C — Secure Update Download

Version: **v0.0.36-beta**

## Added
- Explicit technician-triggered installer download.
- Artifact URL resolved from the accepted HTTPS manifest.
- HTTPS-only artifact transport.
- Download staging under `%LOCALAPPDATA%\ForgeCare\Updates`.
- `.partial` file during transfer.
- SHA-256 verification against the accepted manifest.
- Constant-time hash comparison.
- Automatic deletion of a downloaded artifact when the hash mismatches.
- Progress UI and verified staged-file path.
- Installer execution remains disabled.

## Required manifest fields
The accepted remote manifest must provide:
- `installer.file`
- `installer.sha256`

The artifact may be a relative path beside the manifest or an absolute HTTPS URL.

## Safety boundary
14C performs:

remote manifest validation
→ explicit download
→ SHA-256 verification
→ staged verified file

It does **not** execute the installer.

## Next
Sprint 14D can add an explicit technician-approved installer handoff with a final confirmation screen and clear process-exit behavior.
