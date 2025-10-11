using AnkiNet.CollectionFile.Model;

namespace AnkiNet;

/// <summary>
/// Representa uma entrada de log de revisão de um cartão Anki.
/// </summary>
public readonly record struct AnkiRevLog(
    long Id,                    // Timestamp do evento
    long CardId,                // ID do cartão revisado
    long UpdateSequenceNumber,  // Sequência usada pelo Anki para sincronização
    long Ease,                  // Valor da resposta (1 a 4)
    long Interval,              // Intervalo atual em dias
    long LastInterval,          // Intervalo anterior em dias
    long Factor,                // Fator de aprendizado (multiplicador de intervalo)
    long TimeTookMs,            // Tempo gasto na resposta, em milissegundos
    RevisionType RevisionType   // Tipo de revisão (Review, Learn, Relearn etc.)
)
{
    /// <summary>
    /// Retorna o tipo de facilidade da revisão (Wrong, Hard, Ok, Easy)
    /// de acordo com o tipo de revisão e valor de Ease.
    /// </summary>
    public RevisionEaseType GetEaseType()
    {
        return RevisionType switch
        {
            RevisionType.Review => Ease switch
            {
                1 => RevisionEaseType.Wrong,
                2 => RevisionEaseType.Hard,
                3 => RevisionEaseType.Ok,
                4 => RevisionEaseType.Easy,
                _ => throw new InvalidOperationException($"Ease inválido: {Ease}")
            },
            RevisionType.Learn or RevisionType.Relearn => Ease switch
            {
                1 => RevisionEaseType.Wrong,
                2 => RevisionEaseType.Ok,
                3 => RevisionEaseType.Easy,
                _ => throw new InvalidOperationException($"Ease inválido: {Ease}")
            },
            _ => throw new InvalidOperationException($"Tipo de revisão inválido: {RevisionType}")
        };
    }
}
