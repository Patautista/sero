using Domain;

namespace AspireApp1.Web;

public class ApiClient(HttpClient httpClient)
{
    public async Task<ICollection<Card>> GetCards(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var buffer = await httpClient.GetFromJsonAsync<List<Card>>("/cards", cancellationToken);
        return buffer;
    }
}
