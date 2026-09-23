using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Shared.Models
{
    /// <summary>
    /// A skill area in the learner's progress path. Separate from broad proficiency
    /// skills such as Reading/Writing/Listening: this describes what is being learnt.
    /// </summary>
    public sealed class SkillArea
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Stage { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public List<string> ApplicableTo { get; set; } = new();
        public string Purpose { get; set; } = string.Empty;

        [JsonIgnore]
        public string Category => Id.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        public bool IsApplicableTo(string? languageCode)
        {
            if (ApplicableTo.Contains("*", StringComparer.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            var normalizedCode = languageCode.Split('-', '_')[0];
            return ApplicableTo.Contains(normalizedCode, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// A validated, immutable catalog of available skill areas loaded from JSON.
    /// </summary>
    public sealed class SkillAreaCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IReadOnlyDictionary<string, SkillArea> _areas;

        public SkillAreaCatalog(IEnumerable<SkillArea> areas)
        {
            ArgumentNullException.ThrowIfNull(areas);

            var list = areas.ToList();
            Validate(list);

            _areas = list.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<SkillArea> All => _areas.Values.ToList();

        public SkillArea? Find(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (_areas.TryGetValue(id, out var area))
                return area;

            return _areas.Values.SingleOrDefault(candidate =>
                string.Equals(GetLegacyId(candidate.Id), id, StringComparison.OrdinalIgnoreCase));
        }

        public bool Contains(string? id) => Find(id) is not null;

        public IReadOnlyList<SkillArea> ApplicableTo(string? languageCode) => _areas.Values
            .Where(area => area.IsApplicableTo(languageCode))
            .ToList();

        public static SkillAreaCatalog FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            var areas = JsonSerializer.Deserialize<List<SkillArea>>(json, JsonOptions) ?? new List<SkillArea>();
            return new SkillAreaCatalog(areas);
        }

        private static void Validate(IReadOnlyList<SkillArea> areas)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var area in areas)
            {
                if (string.IsNullOrWhiteSpace(area.Id))
                    throw new InvalidOperationException("Every skill area must have a non-empty Id.");

                if (string.IsNullOrWhiteSpace(area.Name))
                    throw new InvalidOperationException($"Skill area '{area.Id}' must have a non-empty Name.");

                var pathSegments = area.Id.Split('/');
                if (pathSegments.Length < 2 || pathSegments.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException($"Skill area '{area.Id}' must use a category/name path.");

                if (string.IsNullOrWhiteSpace(area.Purpose))
                    throw new InvalidOperationException($"Skill area '{area.Id}' must have a non-empty Purpose.");

                if (area.ApplicableTo.Count == 0 || area.ApplicableTo.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException($"Skill area '{area.Id}' must declare at least one ApplicableTo language code.");

                if (area.ApplicableTo.Contains("*", StringComparer.OrdinalIgnoreCase) && area.ApplicableTo.Count != 1)
                    throw new InvalidOperationException($"Skill area '{area.Id}' must use '*' by itself in ApplicableTo.");

                if (!ids.Add(area.Id))
                    throw new InvalidOperationException($"Duplicate skill area id '{area.Id}'.");
            }

            var duplicateLegacyId = areas
                .GroupBy(area => GetLegacyId(area.Id), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateLegacyId is not null)
                throw new InvalidOperationException($"Skill area leaf id '{duplicateLegacyId.Key}' must be unique for legacy progress compatibility.");

            foreach (var area in areas)
            {
                foreach (var prerequisite in area.Prerequisites.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (!ids.Contains(prerequisite))
                        throw new InvalidOperationException($"Skill area '{area.Id}' references unknown prerequisite '{prerequisite}'.");
                }
            }

            DetectCycles(areas);
        }

        internal static string GetLegacyId(string areaId)
        {
            if (string.Equals(areaId, "home/living-room", StringComparison.OrdinalIgnoreCase))
                return "home";

            var separatorIndex = areaId.LastIndexOf('/');
            return separatorIndex >= 0 ? areaId[(separatorIndex + 1)..] : areaId;
        }

        private static void DetectCycles(IReadOnlyList<SkillArea> areas)
        {
            var map = areas.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var area in areas)
            {
                Visit(area.Id);
            }

            void Visit(string id)
            {
                if (visited.Contains(id))
                    return;

                if (!visiting.Add(id))
                    throw new InvalidOperationException($"Skill area prerequisite cycle detected involving '{id}'.");

                foreach (var prerequisite in map[id].Prerequisites.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    Visit(prerequisite);
                }

                visiting.Remove(id);
                visited.Add(id);
            }
        }
    }

    /// <summary>
    /// Per-area mastery scores (0-100), stored by skill-area id.
    /// </summary>
    public sealed class AreaProgress
    {
        public const int MasteredThreshold = 70;

        private readonly Dictionary<string, int> _scores = new(StringComparer.OrdinalIgnoreCase);

        public int this[string areaId]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(areaId))
                    return SkillProfile.MinScore;

                if (_scores.TryGetValue(areaId, out var score))
                    return score;

                var legacyId = SkillAreaCatalog.GetLegacyId(areaId);
                return _scores.TryGetValue(legacyId, out score) ? score : SkillProfile.MinScore;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(areaId))
                    return;

                _scores[areaId] = SkillProfile.Clamp(value);
                var legacyId = SkillAreaCatalog.GetLegacyId(areaId);
                if (!string.Equals(legacyId, areaId, StringComparison.OrdinalIgnoreCase))
                    _scores.Remove(legacyId);
            }
        }

        public IReadOnlyDictionary<string, int> Scores => _scores;

        public void Adjust(string areaId, int delta) => this[areaId] = this[areaId] + delta;

        public bool IsMastered(string areaId) => this[areaId] >= MasteredThreshold;

        public IReadOnlyList<SkillArea> GetFrontier(
            SkillAreaCatalog catalog,
            string? languageCode = null,
            int take = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            var areas = string.IsNullOrWhiteSpace(languageCode)
                ? catalog.All
                : catalog.ApplicableTo(languageCode);

            return areas
                .Where(area => !IsMastered(area.Id) && area.Prerequisites.All(IsMastered))
                .OrderBy(area => area.Stage)
                .ThenBy(area => this[area.Id])
                .ThenBy(area => area.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(0, take))
                .ToList();
        }

        public IReadOnlyList<SkillArea> GetFrontierByCategory(
            SkillAreaCatalog catalog,
            string category,
            string? languageCode = null,
            int take = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            return GetFrontier(catalog, languageCode, int.MaxValue)
                .Where(area => string.Equals(area.Category, category, StringComparison.OrdinalIgnoreCase))
                .Take(Math.Max(0, take))
                .ToList();
        }

        public Dictionary<string, int> ToStorage() => new(_scores, StringComparer.OrdinalIgnoreCase);

        public string ToJson() => JsonSerializer.Serialize(ToStorage());

        public static AreaProgress FromStorage(IReadOnlyDictionary<string, int>? stored)
        {
            var progress = new AreaProgress();
            if (stored is null)
                return progress;

            foreach (var (areaId, score) in stored)
            {
                progress[areaId] = score;
            }

            return progress;
        }

        public static AreaProgress FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new AreaProgress();

            try
            {
                var stored = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                return FromStorage(stored);
            }
            catch (JsonException)
            {
                return new AreaProgress();
            }
        }
    }
}
