using Business.ETL.Pipelines;
using Business.Pipelines;
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
            "You are a flashcard assistant. \n" +
            "Provide one alternative valid translation in italian to the highlighted sentence in portuguese.\n" +
            "Sentence : %SENTENCE%\n" +
            "Existing Translations: %TRANSLATIONS%\n" +
            "Do NOT return an existing translation\n" +
            "Do NOT provide explanations or commentary. Return a single translation in text.";
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
                var prompt = promptTemplate
                    .Replace("%SENTENCE%", card.NativeSentence.Text)
                    .Replace("%TRANSLATIONS%", card.TargetSentence.Text);

                var res = await _promptClient.GenerateAsync(prompt);
                Console.WriteLine(res);
            }
            throw new NotImplementedException();
        }
    }
}
