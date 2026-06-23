# .NET 10.0 Upgrade Plan

## Overview

**Target**: Upgrade Triolingo solution from .NET 8 to .NET 10 (LTS)
**Scope**: 9 projects (~2k-5k LOC estimated), modern .NET upgrade with package updates and minor API fixes

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 9 projects, all on .NET 8, clean dependency structure (5 levels), straightforward modern-to-modern upgrade.

## Tasks

### 01-prerequisites: Verify SDK and toolchain compatibility

Validate that .NET 10 SDK is installed and compatible with the solution's current configuration. Check global.json files (if any) to ensure SDK version specifications allow .NET 10 workloads, especially for the MAUI project which requires net10.0-android, net10.0-ios, net10.0-maccatalyst, and net10.0-windows workloads.

**Done when**: .NET 10 SDK verified as installed, global.json files updated or validated for compatibility, all required MAUI workloads available

---

### 02-upgrade-all-projects: Update target frameworks, packages, and fix breaking changes

Update all 9 projects to net10.0 (or net10.0-android/ios/maccatalyst/windows for the MAUI project), upgrade all packages to compatible versions, and address breaking API changes identified in the assessment. This includes:

- **Target framework updates**: Change TargetFramework from net8.0 to net10.0 for all 8 non-MAUI projects, and update the multi-targeted MAUI project (Triolingo.csproj) from net8.0-* to net10.0-*
- **Package updates**: Upgrade Microsoft.* packages (ASP.NET Core, Entity Framework Core, Extensions.*, Data.Sqlite) from 8.0.x to 10.0.x versions, add missing MAUI package versions
- **Breaking API fixes**: Address 2 binary incompatibilities in Business and SupportServer projects, 1 source incompatibility and behavioral changes in Infrastructure project, behavioral changes in Triolingo MAUI project
- **Incompatible package resolution**: Replace Microsoft.VisualStudio.Azure.Containers.Tools.Targets (incompatible) with equivalent functionality or remove if not needed
- **Deprecated package handling**: Replace xunit package (deprecated) with modern testing packages or upgrade to non-deprecated version

Assessment identified 125 issues across 36 files: 12 mandatory fixes (binary/source incompatibilities), 112 potential issues (package updates), 1 optional (deprecated package). Work proceeds in this order: update project files → update packages → restore → build and fix compilation errors → verify solution builds with 0 errors.

**Done when**: All projects target net10.0 (or net10.0-* for MAUI), all packages updated to compatible versions, solution builds with 0 errors and 0 warnings, incompatible and deprecated packages resolved

---

### 03-final-validation: Verify solution stability and document recommendations

Run full solution build, execute all test projects (SupportServer.Tests, and any other test projects discovered), verify MAUI project configuration is correct for all platforms. Document any deferred recommendations such as adopting Central Package Management (CPM) now that all projects are on a single modern TFM.

**Done when**: Full solution builds successfully, all tests pass, MAUI project platform configurations validated, post-upgrade recommendations documented in execution-log.md
