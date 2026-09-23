using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.MentalModels
{
    /// <summary>
    /// Orchestrates the mental models. For each turn it asks every registered
    /// <see cref="IMentalModel"/> to contribute its insight, then hands the collected
    /// insights to the <see cref="IConversationStrategist"/> to decide the next
    /// objective. Models never talk to each other — all coordination flows through the
    /// engine — and because the model set is injected, new models (Emotion, Motivation,
    /// Goal, …) participate automatically once registered, with no engine changes.
    /// </summary>
    public sealed class ConversationEngine
    {
        private readonly IReadOnlyList<IMentalModel> _models;
        private readonly IConversationStrategist _strategist;
        private readonly ILogger<ConversationEngine> _logger;

        public ConversationEngine(
            IEnumerable<IMentalModel> models,
            IConversationStrategist strategist,
            ILogger<ConversationEngine> logger)
        {
            _models = models.ToList();
            _strategist = strategist;
            _logger = logger;
        }

        /// <summary>
        /// Runs every mental model and the strategist for this turn and returns the
        /// combined reasoning. Models are executed sequentially because they share the
        /// scoped database context; a single failing model is logged and skipped so one
        /// model can never take down the whole turn.
        /// </summary>
        public async Task<MentalModelReasoning> ReasonAsync(MentalModelRequest request, CancellationToken cancellationToken = default)
        {
            var insights = new List<MentalModelInsight>(_models.Count);

            foreach (var model in _models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var insight = await model.ObserveAsync(request, cancellationToken);
                    if (insight is not null && !string.IsNullOrWhiteSpace(insight.Content))
                        insights.Add(insight);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mental model '{Model}' failed; skipping its contribution this turn.", model.Name);
                }
            }

            ConversationStrategyPlan strategy;
            try
            {
                strategy = await _strategist.PlanAsync(request, insights, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Conversation strategist failed; falling back to a neutral plan.");
                strategy = MentalModelReasoning.Empty.Strategy;
            }

            _logger.LogInformation(
                "Conversation engine produced {Count} insight(s); primary objective: {Objective}",
                insights.Count, strategy.PrimaryObjective);

            return new MentalModelReasoning
            {
                Insights = insights,
                Strategy = strategy
            };
        }
    }
}
