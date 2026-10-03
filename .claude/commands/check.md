---
description: Run the full quality gate (format, build with warnings as errors, tests)
---
Run `pwsh tools/check.ps1` (or on non-Windows: `dotnet format DEADSWITCH.sln --verify-no-changes --severity warn && dotnet build DEADSWITCH.sln -warnaserror && dotnet test DEADSWITCH.sln`). Report failures concisely and fix them. Do not weaken tests.
