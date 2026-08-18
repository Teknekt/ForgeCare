# ForgeCare Sprint 11A — Persistent Technician Profile

Version: v0.0.25-alpha

## Added
- Functional Settings page
- Persistent local technician/company profile
- Default customer and device label
- Auto-fill for empty professional report metadata
- Local settings JSON
- Open ForgeCare data folder button
- Reset-to-default preferences
- Explicit local save feedback

## Test
1. Rebuild Solution.
2. F5.
3. Open SETTINGS.
4. Enter technician/company/defaults.
5. Click SAVE PREFERENCES.
6. Close ForgeCare completely.
7. Start it again.
8. Verify the values survived.
9. Open REPORTS and verify empty report identity fields can be seeded by the saved profile.

Settings are stored locally under:
%LOCALAPPDATA%\ForgeCare\Settings\settings.json
