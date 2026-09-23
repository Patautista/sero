using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MauiApp2.Features.MentalModels.Models
{
    /// <summary>
    /// Represents the companion's own persistent identity so it reacts consistently and
    /// never invents a new personality each conversation. Surfaces the pet's likes,
    /// dislikes, hobbies, dream, fear, opinions and previously shared anecdotes from a
    /// stable identity source. Answers: "What would I enjoy talking about?", "What have
    /// I already told the user?", "How should I react consistently?".
    /// </summary>
    public sealed class PetModel : IMentalModel
    {
        private readonly IPetIdentityProvider _identityProvider;

        public PetModel(IPetIdentityProvider identityProvider)
        {
            _identityProvider = identityProvider;
        }

        public string Name => "Pet Model";

        public Task<MentalModelInsight?> ObserveAsync(MentalModelRequest request, CancellationToken cancellationToken = default)
        {
            var identity = _identityProvider.GetIdentity(request.Companion.Name);
            var sb = new StringBuilder();

            AppendList(sb, "Traits", identity.Traits);
            AppendList(sb, "Likes", identity.Likes);
            AppendList(sb, "Dislikes", identity.Dislikes);
            AppendList(sb, "Hobbies", identity.Hobbies);
            AppendList(sb, "Favourite foods", identity.FavoriteFoods);

            if (!string.IsNullOrWhiteSpace(identity.Dream))
                sb.AppendLine($"Dream: {identity.Dream}");
            if (!string.IsNullOrWhiteSpace(identity.Fear))
                sb.AppendLine($"Fear: {identity.Fear}");

            AppendList(sb, "Opinions you hold", identity.Opinions);
            AppendList(sb, "Things you've already shared", identity.SharedMemories);

            sb.AppendLine("Stay true to this identity; weave it in naturally and don't contradict it.");

            return Task.FromResult<MentalModelInsight?>(new MentalModelInsight(
                Name,
                "PET MODEL — who you are",
                sb.ToString().TrimEnd(),
                Order: 50));
        }

        private static void AppendList(StringBuilder sb, string label, IReadOnlyList<string> values)
        {
            var items = values?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new List<string>();
            if (items.Count > 0)
                sb.AppendLine($"{label}: {string.Join(", ", items)}");
        }
    }
}
