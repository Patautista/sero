namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<object, string>(this, "SharedText", (sender, text) =>
            {
                // Aqui você já cria o Card com o texto
                DisplayAlert("Novo Card", $"Texto recebido: {text}", "OK");
            });
        }
    }
}
