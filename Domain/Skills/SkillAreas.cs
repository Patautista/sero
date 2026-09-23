using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Shared.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SkillAreaKind
    {
        Concept,
        Topic
    }

    /// <summary>
    /// A skill area in the learner's progress path. Separate from broad proficiency
    /// skills such as Reading/Writing/Listening: this describes what is being learnt.
    /// </summary>
    public sealed class SkillArea
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public SkillAreaKind Kind { get; set; }
        public int Stage { get; set; }
        public List<string> Prerequisites { get; set; } = new();
    }

    /// <summary>
    /// A validated, immutable catalog of available skill areas loaded from JSON.
    /// </summary>
    public sealed class SkillAreaCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
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

            return _areas.TryGetValue(id, out var area) ? area : null;
        }

        public bool Contains(string? id) => Find(id) is not null;

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

                if (!ids.Add(area.Id))
                    throw new InvalidOperationException($"Duplicate skill area id '{area.Id}'.");
            }

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
            get => string.IsNullOrWhiteSpace(areaId) ? SkillProfile.MinScore : _scores.TryGetValue(areaId, out var score) ? score : SkillProfile.MinScore;
            set
            {
                if (string.IsNullOrWhiteSpace(areaId))
                    return;

                _scores[areaId] = SkillProfile.Clamp(value);
            }
        }

        public IReadOnlyDictionary<string, int> Scores => _scores;

        public void Adjust(string areaId, int delta) => this[areaId] = this[areaId] + delta;

        public bool IsMastered(string areaId) => this[areaId] >= MasteredThreshold;

        public IReadOnlyList<SkillArea> GetFrontier(SkillAreaCatalog catalog, int take = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            return catalog.All
                .Where(area => !IsMastered(area.Id) && area.Prerequisites.All(IsMastered))
                .OrderBy(area => area.Stage)
                .ThenBy(area => this[area.Id])
                .ThenBy(area => area.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(0, take))
                .ToList();
        }

        public IReadOnlyList<SkillArea> GetFrontierByKind(SkillAreaCatalog catalog, SkillAreaKind kind, int take = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            return GetFrontier(catalog, int.MaxValue)
                .Where(area => area.Kind == kind)
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
