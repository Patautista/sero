# Resetting the Language Pet Database

If you're getting a "table already exists" error, you need to delete the SQLite database file and let the app recreate it.

## Finding the Database File

The database is stored in your app's data directory. The location depends on your platform:

### Android
```
/data/data/com.companyname.mauiapp2/files/languagepet.db
```

### Windows
```
C:\Users\[YourUsername]\AppData\Local\Packages\[PackageId]\LocalState\languagepet.db
```

### iOS/macOS
```
~/Library/Containers/[BundleId]/Data/Library/languagepet.db
```

## Quick Reset Methods

### Method 1: Uninstall and Reinstall (Easiest)

**Android/iOS:**
1. Uninstall the app from your device/emulator
2. Rebuild and run the app from Visual Studio

**Windows:**
1. Right-click the app in Start Menu
2. Select "Uninstall"
3. Rebuild and run from Visual Studio

### Method 2: Add a Development Reset Feature

Add this helper method to your `PetDbContextInitialiser.cs`:

```csharp
public async Task ResetDatabaseAsync()
{
	_logger.LogWarning("RESETTING DATABASE - ALL DATA WILL BE LOST");

	// Delete the database
	await _context.Database.EnsureDeletedAsync();

	// Recreate it
	await _context.Database.EnsureCreatedAsync();

	_logger.LogInformation("Database reset complete");
}
```

Then in your `App.xaml.cs`, you can conditionally call it:

```csharp
#if DEBUG
	// Uncomment to reset database on startup during development
	// await dbInitializer.ResetDatabaseAsync();
#endif
await dbInitializer.InitialiseAsync();
```

### Method 3: Manual File Deletion (Advanced)

For debugging, you can use ADB (Android) or file explorer to manually delete the database:

**Android with ADB:**
```bash
adb shell
run-as com.companyname.mauiapp2
cd files
rm languagepet.db
```

**Windows:**
```powershell
# Find the package folder
Get-AppxPackage *mauiapp2* | Select InstallLocation

# Navigate to LocalState folder and delete the .db file
```

## After Deletion

Once you've deleted the database file:

1. Rebuild the solution (Ctrl+Shift+B)
2. Run the app (F5)
3. The database will be recreated automatically
4. Luna (your companion) will be seeded
5. You'll start fresh at the onboarding screen

## Prevention

The fix I just applied will prevent this error going forward by:
- Checking if the database exists before deciding how to initialize it
- Using `EnsureCreated` for new databases (no migrations)
- Using `Migrate` only for existing databases with pending migrations

## Current Fix Applied

The `PetDbContextInitialiser` now:
1. ✅ Checks if database can connect (exists)
2. ✅ If **new**: uses `EnsureCreated` (fast, simple)
3. ✅ If **existing**: checks for and applies migrations
4. ✅ Logs each step for debugging

**For now:** Just uninstall/reinstall the app to clear the old database and you'll be good to go! 🚀
