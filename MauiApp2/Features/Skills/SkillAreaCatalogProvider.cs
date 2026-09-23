using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.Skills
{
    public interface ISkillAreaCatalogProvider
    {
        Task<SkillAreaCatalog> GetCatalogAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Loads the skill-area catalog from the MAUI app package and caches the parsed,
    /// validated catalog for reuse across the app lifetime.
    /// </summary>
    public sealed class SkillAreaCatalogProvider : ISkillAreaCatalogProvider
    {
        private const string AssetName = "skill-areas.json";

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ILogger<SkillAreaCatalogProvider> _logger;
        private SkillAreaCatalog? _catalog;

        public SkillAreaCatalogProvider(ILogger<SkillAreaCatalogProvider> logger)
        {
            _logger = logger;
        }

        public async Task<SkillAreaCatalog> GetCatalogAsync(CancellationToken cancellationToken = default)
        {
            if (_catalog is not null)
                return _catalog;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (_catalog is not null)
                    return _catalog;

                using var stream = await FileSystem.OpenAppPackageFileAsync(AssetName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync(cancellationToken);
                _catalog = SkillAreaCatalog.FromJson(json);

                _logger.LogInformation("Loaded {Count} skill areas from {AssetName}.", _catalog.All.Count, AssetName);
                return _catalog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load skill area catalog from {AssetName}.", AssetName);
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
