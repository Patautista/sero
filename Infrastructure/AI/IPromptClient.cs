
namespace Infrastructure.AI
{
    public interface IPromptClient
    {
        Task<string> GenerateAsync(string prompt, string model);
    }
}