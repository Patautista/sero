# Language Pet MVP - Setup Instructions

## 🚀 Quick Start

### 1. Configure appsettings.json

```bash
# Copy the template
cp appsettings.json.template appsettings.json
```

Edit `appsettings.json` and add your **Gemini API Key**:

```json
{
  "AI": {
	"GeminiApiKey": "YOUR_ACTUAL_API_KEY_HERE"
  }
}
```

### 2. Get a Gemini API Key

1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click **"Create API Key"**
4. Copy the key and paste it into `appsettings.json`

### 3. Add appsettings.json to .gitignore

**IMPORTANT:** Never commit your API keys!

Add this to `.gitignore`:
```
# Configuration files with secrets
appsettings.json
```

### 4. Build Configuration

In `MauiApp2.csproj`, ensure appsettings.json is included as an embedded resource:

```xml
<ItemGroup>
  <EmbeddedResource Include="appsettings.json" />
</ItemGroup>
```

### 5. Run the App

```bash
dotnet build
dotnet run
```

## 📋 Configuration Options

See `appsettings.json.template` for all available configuration options including:

- **AI Settings**: Model selection, temperature, max tokens
- **Companion**: Personality, name, mood changes
- **Proactive Messages**: Frequency, timing, limits
- **Features**: Enable/disable notifications, voice, memory extraction
- **Performance**: Cache sizes, conversation history limits
- **Development**: Debug flags, test data, skip onboarding

## 🔒 Security Best Practices

1. ✅ **Always** use `appsettings.json.template` for version control
2. ✅ **Never** commit `appsettings.json` with real API keys
3. ✅ Add `appsettings.json` to `.gitignore`
4. ⚠️ For production, consider using Azure Key Vault or similar

## 🛠️ Development Tips

### Test without API calls
```json
{
  "Development": {
	"MockCompanionResponses": true
  }
}
```

### Skip onboarding for testing
```json
{
  "Development": {
	"SkipOnboarding": true
  }
}
```

### Enable debug logging
```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Debug"
	},
	"LogAiRequests": true
  }
}
```

## 🌍 Language Support

Default supported languages (configurable):
- English (en) 🇬🇧
- Spanish (es) 🇪🇸
- French (fr) 🇫🇷
- German (de) 🇩🇪
- Italian (it) 🇮🇹
- Portuguese (pt) 🇵🇹
- Japanese (ja) 🇯🇵
- Korean (ko) 🇰🇷
- Chinese (zh) 🇨🇳
- Russian (ru) 🇷🇺
- Arabic (ar) 🇸🇦
- Hindi (hi) 🇮🇳

## 🐛 Troubleshooting

### "appsettings.json not found"
- Make sure you copied `appsettings.json.template` to `appsettings.json`
- Verify it's marked as `EmbeddedResource` in the `.csproj`

### "Gemini API key is not configured"
- Check that you replaced `your_gemini_api_key_here` with your actual key
- Verify the key is valid at [Google AI Studio](https://makersuite.google.com/)

### "Failed to load configuration"
- Ensure `appsettings.json` is valid JSON (use a JSON validator)
- Check for missing commas or brackets

## 📦 Optional: External TTS API

For higher quality text-to-speech, configure Google Cloud TTS:

```json
{
  "Optional": {
	"GoogleTtsApiKey": "your_google_cloud_tts_key"
  }
}
```

Get a key from: [Google Cloud Console](https://console.cloud.google.com/)

## 🏗️ Project Structure

```
MauiApp2/
├── appsettings.json.template    # Configuration template (commit this)
├── appsettings.json             # Your actual config (DO NOT commit)
├── Services/
│   └── ConfigurationService.cs  # Loads configuration
├── Features/                    # Vertical slices
│   ├── Onboarding/
│   ├── Chat/
│   ├── QuickActions/
│   └── ...
└── Data/
	└── PetDbContext.cs          # Database context
```

## 🎯 Next Steps

1. Complete onboarding in the app
2. Try the quick actions (pronunciation, translation, etc.)
3. Start a conversation with Luna
4. Check the proactive messages after a few hours

## 📚 Additional Resources

- [Gemini API Documentation](https://ai.google.dev/docs)
- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
