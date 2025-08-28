
using Domain;
using Infrastructure;
using Infrastructure.Parsing;

class Program
{
    static async Task Main()
    {
        await TestOpenAi();
    }
    static async Task ProcessTatoeba()
    {
        string filePath = "tatoeba it-pt.tsv";

        string[][] matrix = TsvReader.ReadFile(filePath).DistinctBy(l => l[1]).ToArray();
        var lastMeaningId = 0;

        var cards = matrix.Select((l, index) => new Card
        {
            TargetSentence = new Sentence
            {
                MeaningId = lastMeaningId + index + 1,
                Language = "it",
                Text = l[1]
            },
            NativeSentence = new Sentence
            {
                MeaningId = lastMeaningId + index + 1,
                Language = "pt",
                Text = l[3]
            }
        }).ToList();

        // Print result
        for (int i = 0; i < matrix.Length; i++)
        {
            Console.WriteLine(string.Join(" | ", matrix[i]));
        }
    }
    static async Task TestOpenAi()
    {

    }
}
