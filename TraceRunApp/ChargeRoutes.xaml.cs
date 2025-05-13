namespace TraceRunApp;

public partial class ChargeRoutes : ContentPage
{

    private readonly RouteRepository _routeRepo;
    private readonly MainPage _mainPage;

    public ChargeRoutes(MainPage mainPage)
    {
        InitializeComponent();

        _mainPage = mainPage;

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "routes.db");
        _routeRepo = new RouteRepository(dbPath);

        LoadRoutes();
    }

    private async void LoadRoutes()
    {
        var routes = await _routeRepo.GetAllRoutesAsync();

        foreach (var route in routes)
        {
            var button = new Button
            {
                Text = route.Name,
                BackgroundColor = Colors.LightBlue,
                TextColor = Colors.Black,
                Margin = new Thickness(0, 5)
            };

            // Aqui você define o que acontece ao clicar no botão da rota
            button.Clicked += async (s, e) =>
            {
                await _mainPage.DrawSavedRouteAsync(route.Id);
                await Navigation.PopAsync(); // Volta para a MainPage
            };

            RoutesLayout.Children.Add(button);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}