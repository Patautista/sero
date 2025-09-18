using Business.Audio;
using ElevenLabs;
using Infrastructure.Audio;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

// Add ServerDbContext with environment-specific provider
if (builder.Environment.IsDevelopment())
{
    var sqliteConn = builder.Configuration.GetConnectionString("Sqlite");
    builder.Services.AddDbContext<ServerDbContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    var pgConn = builder.Configuration.GetConnectionString("Postgres");
    builder.Services.AddDbContext<ServerDbContext>(options =>
        options.UseNpgsql(pgConn));
}

var app = builder.Build();

// Run EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    db.Database.Migrate();
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
