# Upgrade Options — Triolingo Solution

Assessment: 9 projects, all on net8.0, upgrading to net10.0 (LTS), 125 issues (mostly package updates)

## Strategy

### Upgrade Strategy
All projects are on modern .NET with a clean dependency hierarchy. The scope is manageable and the upgrade is mechanical (TFM bump + package updates).

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all projects simultaneously in a single atomic pass — fastest approach with no multi-targeting overhead |
| Top-Down | Upgrade applications first with multi-targeting on libraries — better for large-scale migrations or when CI must stay green |
