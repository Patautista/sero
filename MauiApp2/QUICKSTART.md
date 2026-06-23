# Language Pet MVP - Quick Start Guide

## 🚀 You're Almost Ready!

The app structure is complete. Here's what you need to do to run it:

## Step 1: Get Gemini API Key

1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click **"Create API Key"**
4. Copy your API key

## Step 2: Configure App

1. Open `MauiApp2/appsettings.json`
2. Replace `YOUR_GEMINI_API_KEY_HERE` with your actual API key:

```json
{
  "AI": {
	"GeminiApiKey": "YOUR_ACTUAL_KEY_HERE",
	...
  }
}
```

## Step 3: Build & Run

```bash
cd MauiApp2
dotnet build
dotnet run
```

Or in Visual Studio:
- Set **MauiApp2** as startup project
- Press **F5** to run

## ✅ What Works Now

### Implemented Features:
- ✅ **Onboarding Flow** - 6-step user setup
- ✅ **Home Page** - 5 quick action buttons
  - Pronunciation helper
  - Translation
  - Word meaning lookup  
  - Text analysis
  - Start conversation
- ✅ **Chat Interface** - Talk with Luna (your AI companion)
  - Real-time conversation
  - Mistake detection & correction
  - Message history
  - Text-to-speech

### Database & Backend:
- ✅ SQLite database (auto-created)
- ✅ User profiles
- ✅ Conversation storage
- ✅ Mistake tracking
- ✅ Memory system

## 🔧 Troubleshooting

### "appsettings.json not found"
- Make sure you're in the MauiApp2 project
- Verify the file exists and contains valid JSON

### "Gemini API key is not configured"
- Check that you replaced the placeholder in appsettings.json
- Verify the key is valid at [Google AI Studio](https://makersuite.google.com/)

### Database errors
- The app creates the database automatically on first run
- Database location: `{AppDataDirectory}/languagepet.db`

### Build errors
- Ensure you have .NET 10 SDK installed
- Run `dotnet restore` in the MauiApp2 folder

## 📱 Platform Support

- ✅ Android (API 24+)
- ✅ iOS (15.0+)
- ✅ Windows (10.0.17763+)
- ✅ macOS (Catalyst 15.0+)

## 🎯 Testing the App

1. **Onboarding**
   - Enter your name
   - Select target language (e.g., Spanish)
   - Select native language (e.g., English)
   - Choose at least 3 interests
   - Meet Luna!

2. **Quick Actions**
   - Try "How do I say..." to translate a phrase
   - Try "Help me read something" to analyze text

3. **Chat**
   - Click "Start a conversation"
   - Type a message in your target language
   - Luna will respond and correct any mistakes

## ⚙️ Configuration Options

In `appsettings.json`, you can customize:

```json
{
  "Features": {
	"EnableMistakeTracking": true,    // Track recurring mistakes
	"EnableMemoryExtraction": true,   // Remember conversation facts
	"EnableVoiceFeatures": true       // Text-to-speech
  },
  "Development": {
	"SkipOnboarding": false,          // Set true to skip onboarding
	"MockCompanionResponses": false,  // Set true to test without API
	"ShowDebugInfo": false            // Show debug information
  }
}
```

## 📊 Features NOT Yet Implemented

These were skipped for the minimal MVP:

- ⏳ Proactive messages (background service)
- ⏳ Push notifications
- ⏳ Activity pattern learning
- ⏳ Companion mood changes
- ⏳ Memory extraction automation

These can be added later by implementing steps 17-19 from the plan.

## 🔐 Security Note

**IMPORTANT:** Never commit `appsettings.json` with your real API key!

Add to `.gitignore`:
```
appsettings.json
```

The template file (`appsettings.json.template`) is safe to commit.

## 📚 Architecture

The app uses **Vertical Slices Architecture**:

```
MauiApp2/
├── Features/              # Self-contained features
│   ├── Onboarding/
│   ├── Chat/
│   ├── QuickActions/
│   ├── LanguageCoaching/
│   └── Memory/
├── Services/              # Shared services
│   ├── LocalApiService.cs
│   ├── ConfigurationService.cs
│   └── NotificationService.cs
├── Data/                  # Database
│   ├── PetDbContext.cs
│   └── TableModels.cs
└── Shared/                # Shared models & components
	└── Models/
		└── DomainModels.cs
```

Each feature is independent and can be developed/tested separately!

## 🐛 Known Issues

1. **TTS may not work on all platforms** - The app uses platform TTS, quality varies
2. **No offline mode** - Requires internet for AI features
3. **Memory extraction is manual** - Run after conversations, not automatic yet

## 🎉 Next Steps

After testing the MVP:
1. Fine-tune AI prompts in the service files
2. Add more languages to the onboarding
3. Implement proactive messages (steps 17-19)
4. Add user profile settings page
5. Implement progress tracking dashboard

## 🙋 Need Help?

- Check the README.md for detailed configuration
- Review the plan document for architecture details
- Look at inline code comments for implementation notes

Happy language learning! 🌙✨
