using Business.ETL.Pipelines;
using Business.Pipelines;
using DeepL;
using Domain.Entity;
using Infrastructure.AI;
using Infrastructure.ETL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ETL.Pipelines
{
    public class TatoebaPipeline : PipelineDefinition
    {
        public TatoebaPipeline()
        {
            Name = nameof(TatoebaPipeline);
            Stages = new List<PipelineStage>
            {
                
            };
        }
    }
    public class GenerateAlternativeSentences : PipelineStage
    {
        private readonly FileDatalakeService _datalakeService;
        private readonly IPromptClient _promptClient;
        private readonly int _batchSize = 30;
        private string promptTemplate =
            "You are a precise flashcard assistant.\n" +
            "Your task is to generate ONE alternative valid translation in Italian for the following sentence in Portuguese.\n" +
            "The translation must:\n" +
            "- Be natural and commonly used by native speakers.\n" +
            "- Be semantically equivalent to the original Portuguese sentence.\n" +
            "- NOT be identical to any of the existing translations.\n" +
            "- NOT include explanations, notes, or formatting. Output only the translation text.\n" +
            "\n" +
            "Portuguese sentence: %SENTENCE%\n" +
            "Existing translations: %TRANSLATIONS%\n" +
            "Alternative translation:";
        public GenerateAlternativeSentences(FileDatalakeService fileDatalakeService, IPromptClient promptClient)
        {
            Name = nameof(GenerateAlternativeSentences);
            _datalakeService = fileDatalakeService;
            _datalakeService.Configure(this);
            _promptClient = promptClient;
        }
        public override async Task ExecuteAsync()
        {
            var cards = _datalakeService.GetData<List<CardSeed1>>();
            var batch = new BatchResult { BatchSize = _batchSize, Schema = "", StartTime = DateTime.Now };
            var count = _datalakeService.BatchCount();
            batch.Id += $"_{count + 1}";
            var data = new List<Card>();
            try
            {
                foreach (var cardSeed in cards.Skip(count * _batchSize).Take(_batchSize))
                {

                    var card = cardSeed.ToDomain();
                    if (card.NativeSample.Text.Split(" ").Length > 2)
                    {
                        var newSentence = await GenerateWithAI(cardSeed);
                        if (!card.SentencesInTargetLanguage.Select(s => s.Text).Contains(newSentence))
                        {
                            card.SentencesInTargetLanguage.Add(
                                new Sentence
                                {
                                    Language = card.TargetSample.Language,
                                    MeaningId = card.TargetSample.MeaningId,
                                    Text = newSentence.Replace("\n", "").Replace("\r", "")
                                });
                        }
                    }
                    data.Add(card);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (data.Count() < _batchSize)
                {
                    batch.MarkAsIncomplete(data);
                }
                else
                {
                    batch.MarkAsComplete(data);
                }
                _datalakeService.SaveBatch(batch);
            }
        }
        public async Task<string> GenerateWithAI(CardSeed1 cardSeed)
        {
            var prompt = promptTemplate
                    .Replace("%SENTENCE%", cardSeed.NativeSentence.Text)
                    .Replace("%TRANSLATIONS%", cardSeed.TargetSentence.Text);

            var res = await _promptClient.GenerateAsync(prompt);
            return res;
        }
    }
}
