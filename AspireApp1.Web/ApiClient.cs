using Domain;

namespace AspireApp1.Web;

public class ApiClient(HttpClient httpClient)
{
    public async Task<ICollection<Sentence>> GetSentences(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var buffer = await httpClient.GetFromJsonAsync<List<Sentence>>("/sentences", cancellationToken);
        return buffer;
    }
}
