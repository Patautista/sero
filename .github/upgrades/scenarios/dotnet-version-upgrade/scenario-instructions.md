# .NET Version Upgrade to .NET 10.0

## Strategy
All-At-Once — All projects upgraded simultaneously in a single operation.

### Execution Constraints
- Single atomic upgrade — all projects updated together
- Validate full solution build after upgrade
- No tier ordering or phased rollout — all 9 projects upgrade in one pass
- MAUI project requires special handling for multi-targeted frameworks (net10.0-android/ios/maccatalyst/windows)

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (.NET 10.0 LTS)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Source Control
- **Source Branch**: master
- **Working Branch**: dotnet-version-upgrade
- **Commit Strategy**: Single Commit at End
