using Business.Audio;
using ElevenLabs;
using Infrastructure.Audio;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<SpeechService>();
builder.Services.AddSingleton<IAudioCache>(new FileAudioCache("voice_cache"));
builder.Services.AddScoped<ElevenLabsClient>(sp =>
{
    var apiKey = builder.Configuration.GetSection("ElevenLabs:ApiKey").Value;
    return new ElevenLabsClient(apiKey);
});

var app = builder.Build();

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
