# 02-upgrade-all-projects: Update target frameworks, packages, and fix breaking changes

Update all 9 projects to net10.0 (or net10.0-android/ios/maccatalyst/windows for the MAUI project), upgrade all packages to compatible versions, and address breaking API changes identified in the assessment. This includes:

- **Target framework updates**: Change TargetFramework from net8.0 to net10.0 for all 8 non-MAUI projects, and update the multi-targeted MAUI project (Triolingo.csproj) from net8.0-* to net10.0-*
- **Package updates**: Upgrade Microsoft.* packages (ASP.NET Core, Entity Framework Core, Extensions.*, Data.Sqlite) from 8.0.x to 10.0.x versions, add missing MAUI package versions
- **Breaking API fixes**: Address 2 binary incompatibilities in Business and SupportServer projects, 1 source incompatibility and behavioral changes in Infrastructure project, behavioral changes in Triolingo MAUI project
- **Incompatible package resolution**: Replace Microsoft.VisualStudio.Azure.Containers.Tools.Targets (incompatible) with equivalent functionality or remove if not needed
- **Deprecated package handling**: Replace xunit package (deprecated) with modern testing packages or upgrade to non-deprecated version

Assessment identified 125 issues across 36 files: 12 mandatory fixes (binary/source incompatibilities), 112 potential issues (package updates), 1 optional (deprecated package). Work proceeds in this order: update project files → update packages → restore → build and fix compilation errors → verify solution builds with 0 errors.

**Done when**: All projects target net10.0 (or net10.0-* for MAUI), all packages updated to compatible versions, solution builds with 0 errors and 0 warnings, incompatible and deprecated packages resolved
