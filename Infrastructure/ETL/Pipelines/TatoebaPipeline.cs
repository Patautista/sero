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
        private readonly FileDatalakeService _fileDatalakeService;
        private readonly IPromptClient _promptClient;
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
            _fileDatalakeService = fileDatalakeService;
            _fileDatalakeService.Configure(this);
            _promptClient = promptClient;
        }
        public override async Task ExecuteAsync()
        {
            var cards = _fileDatalakeService.GetData<List<CardSeed>>();
            foreach (var card in cards) {

                for(var i = 0; i < 3; i++)
                {
                    var res = await GenerateWithTranslator(card);
                    Console.WriteLine(res);
                }
            }
            throw new NotImplementedException();
        }
        public async Task<string> GenerateWithAI(CardSeed cardSeed)
        {
            var prompt = promptTemplate
                    .Replace("%SENTENCE%", cardSeed.NativeSentence.Text)
                    .Replace("%TRANSLATIONS%", cardSeed.TargetSentence.Text);

            var res = await _promptClient.GenerateAsync(prompt);
            return res;
        }
        public async Task<string> GenerateWithTranslator(CardSeed cardSeed)
        {
            var client = new DeepLClient("");
            var translatedText = await client.TranslateTextAsync(
                cardSeed.TargetSentence.Text,
                null,
                LanguageCode.PortugueseBrazilian);

            return translatedText.Text;
        }
    }
}
