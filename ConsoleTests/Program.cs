
using Infrastructure;

class Program
{
    static async Task Main()
    {
        var client = new WiktionaryClient();

        var res = await client.GetHtmlVerbInflectionTable("avere");
        Console.WriteLine(res);
    }
}
