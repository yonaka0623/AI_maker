namespace AiCharacterMaker.Controls;

public partial class FooterBar : ContentView
{
    // characterId を外から受け取るプロパティ
    // （TSの props に相当）
    public string? CharacterId { get; set; }

    public FooterBar()
    {
        InitializeComponent();
    }

    async void OnHomeTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main");
    }

    async void OnChatTapped(object sender, EventArgs e)
    {
        var route = CharacterId != null
            ? $"//chat?characterId={CharacterId}"
            : "//chat";
        await Shell.Current.GoToAsync(route);
    }

    async void OnCharaListTapped(object sender, EventArgs e)
    {
        var route = CharacterId != null
            ? $"//charaList?characterId={CharacterId}"
            : "//charaList";
        await Shell.Current.GoToAsync(route);
    }

    async void OnMemoryTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//memory");
    }
}