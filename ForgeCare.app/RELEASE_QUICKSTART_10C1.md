# ForgeCare v0.0.24-alpha — Release Quick Start

## Important distinction

The **Report Details SAVE button has nothing to do with PowerShell publishing**.

Report metadata is saved by the running ForgeCare application into the active
local report session. Sprint 10C.1 adds an explicit green `SAVED LOCALLY` status
so a successful save is visible.

The PowerShell signing error is caused by **PowerShell execution policy** for the
local `.ps1` release script. It is not an indication that the ForgeCare report
feature failed, and it is not the same thing as signing `ForgeCare.exe`.

## Recommended release command

Double-click:

`scripts\publish-win-x64.cmd`

The CMD launcher starts the local PowerShell script with:

`-ExecutionPolicy Bypass`

for **that PowerShell process only**. It does not permanently lower the current
user or machine execution policy.

## Alternative manual PowerShell command

From the ForgeCare project root:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\scripts\publish-win-x64.ps1
```

Closing that PowerShell window removes the process-scoped policy change.

## Expected output

- `artifacts\ForgeCare-win-x64\`
- `artifacts\ForgeCare-v0.0.24-alpha-win-x64.zip`

## About digital signing

A production-trusted Windows executable/installer requires a real code-signing
certificate and signing step. Sprint 10C.1 does **not** fake that.

For local alpha testing, the portable self-contained build is the intended path.
Code signing belongs in the later installer/public release hardening pass.
