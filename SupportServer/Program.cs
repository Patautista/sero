using Business.Audio;
using DeepL;
using ElevenLabs;
using Google.Cloud.TextToSpeech.V1;
using Infrastructure.AI;
using Infrastructure.Audio;
using Infrastructure.Data;
using Infrastructure.Data.Model.Server;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ISpeechService, GoogleSpeechService>();
builder.Services.AddScoped<TextToSpeechClient>(sp =>
{
    return TextToSpeechClient.Create();
});
builder.Services.AddScoped<DeepLClient>(sp =>
{
    var apiKey = builder.Configuration.GetSection("DeepL:ApiKey").Value;
    return new DeepLClient(apiKey);
});
builder.Services.AddScoped<IPromptClient, GeminiClient>(sp =>
{
    var apiKey = builder.Configuration.GetSection("Gemini:ApiKey").Value;
    return new GeminiClient(apiKey);
});
builder.Services.AddSingleton<IAudioCache>(new FileAudioCache("voice_cache"));
builder.Services.AddScoped<ElevenLabsClient>(sp =>
{
    var apiKey = builder.Configuration.GetSection("ElevenLabs:ApiKey").Value;
    return new ElevenLabsClient(apiKey);
});

// Configure Cloudflare R2 Client
var cloudflareConfig = builder.Configuration.GetSection("CloudflareR2").Get<CloudflareR2Config>();
builder.Services.AddSingleton(sp =>
{
    if (cloudflareConfig == null)
        throw new InvalidOperationException("CloudflareR2 configuration is missing");
        
    return new CloudflareR2Client(
        cloudflareConfig.AccountId,
        cloudflareConfig.AccessKeyId,
        cloudflareConfig.SecretAccessKey,
        cloudflareConfig.BucketName
    );
});

// Use in-memory database instead of SQLite/PostgreSQL
builder.Services.AddDbContext<ServerDbContext>(options =>
    options.UseInMemoryDatabase("ServerDb"));

var app = builder.Build();

// Load API access data from Cloudflare R2 at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    var r2Client = scope.ServiceProvider.GetRequiredService<CloudflareR2Client>();
    
    try
    {
        // Download API access data from Cloudflare R2
        var apiAccessFile = cloudflareConfig?.ApiAccessFile ?? "ApiAccess.json";
        var apiAccessList = await r2Client.DownloadJsonAsync<List<ApiAccess>>(apiAccessFile);
        
        if (apiAccessList != null && apiAccessList.Any())
        {
            // Populate in-memory database
            await db.ApiAccesses.AddRangeAsync(apiAccessList);
            await db.SaveChangesAsync();
            
            app.Logger.LogInformation($"Loaded {apiAccessList.Count} API access records from Cloudflare R2");
        }
        else
        {
            app.Logger.LogWarning("No API access records found in Cloudflare R2");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to load API access data from Cloudflare R2");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
