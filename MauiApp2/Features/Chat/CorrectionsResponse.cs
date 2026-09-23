using System.Collections.Generic;
using Domain.Shared.Models;

namespace MauiApp2.Features.Chat
{
    public class CorrectionsResponse
    {
        public List<CorrectionData> Corrections { get; set; } = new();
        public List<string> NewlyMasteredConcepts { get; set; } = new();
    }
}
