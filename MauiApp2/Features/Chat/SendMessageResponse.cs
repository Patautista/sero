namespace MauiApp2.Features.Chat
{
    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public ChatMessage UserMessage { get; set; }
        public ChatMessage CompanionResponse { get; set; }
        public string ErrorMessage { get; set; }
    }
}
