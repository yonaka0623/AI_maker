namespace AICharacterMaker.Controls
{
    public partial class FooterBar : ContentView
    {
        public FooterBar()
        {
            InitializeComponent();
        }

        private async void OnCharaListClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CharaListPage");
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("CharaCreatePage");
        }
    }
}
