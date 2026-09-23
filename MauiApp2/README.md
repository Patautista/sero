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

## 🧠 Mental Model Architecture

Instead of stuffing everything into one generic prompt, the companion's reasoning is split into specialized, single-responsibility **mental models** that each answer one class of question. A **Conversation Engine** orchestrates them every turn, and a **Conversation Strategist** decides what that turn should accomplish. This lives under `Features/MentalModels/` and favors long-term consistency over ad-hoc, isolated replies.

### The models

| Model | Answers | Backed by |
|---|---|---|
| **Skill Model** | "What can this user comfortably do? What should be practised next?" | `SkillProfile` scores + recurring mistake patterns |
| **User Model** | "What topics interest the user? What shouldn't be asked again?" | Profile interests + durable memory facts (`Interest`, `Preference`, `Goal`) |
| **Pet Model** | "Who am I? What have I already told the user?" | A stable, code-defined `PetIdentity` (traits, likes, dislikes, hobbies, dream, fear, opinions) |
| **Relationship Model** | "How close are we? What's OK to say?" | `RelationshipState`, derived deterministically from message count + account tenure + mood |
| **Conversation Memory** | "What have we talked about recently?" | Episodic (`Event`) memories + the tail of the current conversation |

Each model implements `IMentalModel.ObserveAsync(...)` and returns a single `MentalModelInsight` (a titled block of text) or `null` when it has nothing relevant to add. Models never call each other — all coordination flows through the engine — so a new model (Emotion, Motivation, Goal, …) can be added later with just one new class and one DI registration.

### The Conversation Strategist

The `ConversationStrategist` is the planner: it looks at the Skill Model's data and the user's interests to decide the next objective, then degrades gracefully:

1. **Practise the weakest skill** when it's below the practice threshold (score < 60), optionally themed around the user's top interest and paired with a matching activity.
2. **Learn something new about the user** when little is known yet.
3. **Have a warm, casual conversation** anchored on a known interest, once skills are solid and the user is known.

The result is a `ConversationStrategyPlan` (primary/secondary objective, suggested action, reasoning).

### The Conversation Engine

`ConversationEngine.ReasonAsync(...)` runs every registered `IMentalModel` sequentially, collects their insights (skipping and logging any that fail, so one bad model can't break a turn), then hands the insights to the strategist. The combined result is a `MentalModelReasoning` — every model's insight plus the strategy plan.

### How it reaches the prompt

`ChatService.BuildResponseContextAsync` materializes a `MentalModelRequest` from already-loaded conversation/user data, calls the engine, and stores the result on `CompanionResponseContext.Reasoning`. `CompanionPromptBuilder` then renders it into two prompt sections — **MENTAL MODELS** (each insight) and **CONVERSATION STRATEGY** (the plan) — instructing the companion to pursue the primary objective this turn while staying consistent with its own identity and the relationship's social permissions. If reasoning couldn't be produced for any reason, the prompt builder falls back to its legacy inline sections so a reply is never blocked.

```
ChatService → ConversationEngine → [SkillModel, UserModel, PetModel, RelationshipModel, ConversationMemoryModel] → ConversationStrategist
                                                                ↓
                                                   MentalModelReasoning → CompanionPromptBuilder → AI prompt
```

## 💡 Living-Relationship Notifications

As you chat with your companion, the system tracks what it learns about you and what you learn about it. Three types of in-chat notifications reflect this evolving relationship:

### **User Insight** 🧠 (Purple pill)
Fires when the companion discovers a **genuinely new, durable fact about you** that wasn't already known (e.g., a new interest, preference, or goal). The fact is automatically saved to memory so it won't be "re-discovered" on future turns.

**Example:**
```
💡 Blip has learned a new thing about you: loves hiking on weekends
```

### **Companion Insight** ✨ (Teal pill)
Fires when the companion **shares a durable new fact about itself** consistent with its identity (e.g., a like, dream, opinion, or past experience).

**Example:**
```
✨ You've learned a new thing about Blip: dreams of visiting the ocean
```

### **Skill Update** 📈 (Green pill)
Fires after you complete a learning activity and your skill scores change.

**Example:**
```
📈 Blip has updated your skill profile: Reading +5, Writing -2
```

All three notices appear as **centered system pills** in the chat, visually distinct from normal conversation. They're designed to emphasize long-term consistency: once a fact is learned, the companion remembers it and the User Model stays coherent across conversations.

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
