using ApplicationL.Model;
using ApplicationL.ViewModel;
using Domain;
using System.Net.Http.Json;

namespace AppLogic.Web;

public class ApiClient(HttpClient httpClient)
{
    public async Task<ICollection<CardWithState>> GetCards(CancellationToken cancellationToken = default)
    {
        var buffer = await httpClient.GetFromJsonAsync<List<CardWithState>>("/cards", cancellationToken);
        return buffer;
    }
    public async Task UpdateUserCardState(UserCardState userCardState,CancellationToken cancellationToken = default)
    {
        await httpClient.PutAsJsonAsync("/cards", userCardState);
    }
}
