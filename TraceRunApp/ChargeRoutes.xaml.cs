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

    //private async void LoadRoutes()
    //{
    //    var routes = await _routeRepo.GetAllRoutesAsync();

    //    foreach (var route in routes)
    //    {
    //        var button = new Button
    //        {
    //            Text = route.Name,
    //            BackgroundColor = Colors.LightBlue,
    //            TextColor = Colors.Black,
    //            Margin = new Thickness(0, 5)
    //        };

    //        // Aqui você define o que acontece ao clicar no botão da rota
    //        button.Clicked += async (s, e) =>
    //        {
    //            await _mainPage.DrawSavedRouteAsync(route.Id);
    //            await Navigation.PopAsync(); // Volta para a MainPage
    //        };

    //        RoutesLayout.Children.Add(button);
    //    }
    //}

    private async void LoadRoutes()
    {
        RoutesLayout.Children.Clear();

        var routes = await _routeRepo.GetAllRoutesAsync();

        foreach (var route in routes)
        {
            // Botão de carregar rota
            var loadButton = new Button
            {
                Text = route.Name,
                BackgroundColor = Color.FromArgb("#BBDEFB"), // Azul clarinho
                TextColor = Colors.Black,
                CornerRadius = 10,
                HeightRequest = 50,
                FontSize = 16
            };

            loadButton.Clicked += async (s, e) =>
            {
                await _mainPage.DrawSavedRouteAsync(route.Id);
                await Navigation.PopAsync();
            };

            // Botão de deletar rota

            var deleteButton = new Button
            {
                ImageSource = "delete.png", // Ícone de deletar
                BackgroundColor = Colors.IndianRed,
                TextColor = Colors.White,
                CornerRadius = 10,
                WidthRequest = 60,
                HeightRequest = 50
            };

            deleteButton.Clicked += async (s, e) =>
            {
                bool confirm = await DisplayAlert(
                    "Confirmar exclusão",
                    $"Deseja realmente apagar a rota \"{route.Name}\"?",
                    "Sim",
                    "Não"
                );

                if (confirm)
                {
                    await _routeRepo.DeleteRouteAsync(route.Id);
                    LoadRoutes(); // Recarrega a lista após deletar
                }
            };

            // Layout com Grid para alinhamento horizontal
            var buttonLayout = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(60) }
            },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 5),
                Padding = 0
            };

            buttonLayout.Add(loadButton, 0, 0);
            buttonLayout.Add(deleteButton, 1, 0);

            // Adiciona o layout à pilha de rotas
            RoutesLayout.Children.Add(buttonLayout);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}