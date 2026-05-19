namespace AICharacterMaker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("CharaListPage", typeof(Pages.CharaListPage));
            Routing.RegisterRoute("ChatPage", typeof(Pages.ChatPage));
            Routing.RegisterRoute("CharaCreatePage", typeof(Pages.CharaCreatePage));
        }
    }
}
