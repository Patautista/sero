# Entity Framework Migrations for MAUI Projects

## Why Migrations Don't Work Directly with MAUI

.NET MAUI projects target multiple platforms (Android, iOS, Windows, macOS) and don't generate the standard `deps.json` file that EF Core migrations tools expect. This is a known limitation.

## Current Solution: Runtime Database Creation

The Language Pet app uses **code-first runtime database creation** via `PetDbContextInitialiser`:

```csharp
public class PetDbContextInitialiser
{
	public async Task InitialiseAsync()
	{
		await _db.Database.EnsureCreatedAsync();
		// Seeds initial data (Luna companion)
	}
}
```

This approach:
- ✅ **Works perfectly for MAUI apps**
- ✅ **Creates the schema automatically on first run**
- ✅ **Seeds initial data (Luna companion)**
- ✅ **No migration files needed**
- ✅ **Schema matches your DbContext models**

## Alternative: Generate Migrations (If Needed)

If you need migration files for documentation or version control, you have two options:

### Option 1: Use a Separate Class Library

1. Create a new .NET 10 class library project
2. Move `PetDbContext` and models to the library
3. Reference the library from MauiApp2
4. Run migrations against the library project

### Option 2: Manual Migration Creation

Since we have a design-time factory (`PetDbContextFactory.cs`), you can create migrations manually:

1. **Extract Schema SQL**:
   ```bash
   # Run the app once to create the database
   # Then find the SQLite file at: {AppDataDirectory}/languagepet.db
   # Use SQLite tools to export the schema
   ```

2. **Create Migration Class**:
   ```csharp
   // MauiApp2/Migrations/20250131000000_InitialCreate.cs
   public partial class InitialCreate : Migration
   {
	   protected override void Up(MigrationBuilder migrationBuilder)
	   {
		   // Add your CREATE TABLE statements here
	   }

	   protected override void Down(MigrationBuilder migrationBuilder)
	   {
		   // Add DROP TABLE statements here
	   }
   }
   ```

## Recommended Approach for This MVP

**Stick with `PetDbContextInitialiser`** because:

1. **Simpler**: No migration complexity
2. **Faster**: Immediate schema updates when models change
3. **MAUI-friendly**: Designed for multi-platform apps
4. **Sufficient for MVP**: Schema is still evolving

## When You Migrate to a Server

When you eventually port `LocalApiService` to a real backend server:

1. The server project will be a standard ASP.NET Core app
2. You CAN run normal EF migrations there
3. The MAUI app will just call the server API
4. The server database will have full migration support

## Current Database Schema

The schema is defined by these table models in `Data/TableModels.cs`:

- `UserProfiles` - User information and language preferences
- `Companions` - AI companion data (Luna)
- `Conversations` - Chat sessions
- `Messages` - Chat messages with metadata
- `LanguageMistakes` - Tracked language errors
- `ConversationMemories` - Extracted facts from conversations
- `UserActivityLogs` - Activity timing data for proactive messages

All relationships, indexes, and constraints are configured in `PetDbContext.cs`.

## Summary

✅ **No action needed!** The database initialization is already set up and working.

The app will create and seed the database automatically on first run through `PetDbContextInitialiser`, which is called in `App.xaml.cs`.
