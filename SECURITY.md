# ForgeCare Security Policy

## Reporting a security issue

Please do **not** publish sensitive security findings, credentials, private machine information or unreviewed diagnostic bundles in a public GitHub issue.

For a suspected security vulnerability, contact MindForge Studio privately at **pontus.lindh@outlook.com** with:

- ForgeCare version
- affected Windows version/environment
- a concise description of the issue
- reproduction steps or proof of concept when safe to provide
- potential impact

Please allow reasonable time for investigation before public disclosure.

## Local data and diagnostic exports

ForgeCare is designed as a local-first desktop application. Depending on the workflow, local ForgeCare data can include settings, report information, safety journal information and diagnostic metadata.

Debug Bundles and exported reports are created to help technicians inspect or communicate system state. They can contain local machine or ForgeCare metadata. **Always review exported files before sharing them.**

Do not attach credentials, secrets, customer-sensitive information or personally identifiable information to public issues.

## Supported public release

Security reports should normally be reproduced against the latest public ForgeCare release when possible. The current public release is **v1.0.0**.

ForgeCare is an independently developed technician tool. Users remain responsible for normal backup, change-control and endpoint-security practices on systems where it is used.
