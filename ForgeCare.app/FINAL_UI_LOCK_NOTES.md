# ForgeCare v1.0 — FINAL UI LOCK

This is the final presentation/release pass before fresh screenshots and website publication.

## Locked changes
- Forge Plan workflow rail now occupies its own row above the recommended actions.
- Plan cards stretch across the workspace instead of clustering on the left.
- Selected actions receive a restrained Forge-gold highlight.
- Plan typography/readability was increased slightly.
- Remaining visible Sprint / release-candidate / alpha-development copy was replaced with product-facing language.
- Public dashboard sample-session control is hidden while its support code remains intact.
- Stable v1 defaults remote update discovery to the `stable` channel.
- New installations also default RemoteUpdateSettings to `stable`.

## UI lock rule
After this package builds and the smoke test passes, do not reopen MainWindow.xaml for cosmetic changes before screenshots.
Only a release-blocking defect should break the lock.
