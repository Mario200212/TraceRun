using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;

namespace TraceRunApp;

public partial class MainPage : ContentPage
{

    private readonly RouteRepository _routeRepo =
    new RouteRepository(Path.Combine(FileSystem.AppDataDirectory, "routes.db"));

    private bool _hasCenteredOnce = false;
    public bool _isDrawingMode = false;
    // Variáveis para armazenar a latitude e longitude do centro
    private double _latitude = -22.817;
    private double _longitude = -47.0632;

    // Serviços
    private readonly LocationService _locationService = new LocationService();
    private MapManager _mapManager;

    public MainPage()
    {
        InitializeComponent();

        _mapManager = new MapManager(MapView);

        MapView.SizeChanged += OnMapControlSizeChanged;
        MapView.Info += OnMapClicked;

        _locationService.LocationUpdated += location =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateLocation(location.Latitude, location.Longitude);
                _mapManager.UpdateSegmentHighlight(location.Latitude, location.Longitude);
            });
        };

        // Chamada aqui para obter e desenhar localização inicial
        InitializeLocationAsync();
    }

    private void OnClearPathClicked(object sender, EventArgs e)
    {
        _mapManager.ClearVertices(); // Chama a função que limpa o mapa
    }

    private async void InitializeLocationAsync()
{
    var location = await _locationService.GetCurrentLocationAsync();

    if (location != null)
    {
        _latitude = location.Latitude;
        _longitude = location.Longitude;

        _mapManager.CenterMap(_latitude, _longitude);
        _mapManager.DrawLocationPin(_longitude, _latitude);
    }
    else
    {
        Console.WriteLine("Localização inicial não encontrada.");
    }
}

    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        var location = await _locationService.GetCurrentLocationAsync();

        if (location != null)
        {
            LocationLabel.Text = $"Lat: {location.Latitude}, Lon: {location.Longitude} ";

            SetCenter(location.Latitude, location.Longitude);

            _mapManager.CenterMap(location.Latitude, location.Longitude);
            _mapManager.DrawLocationPin(location.Longitude, location.Latitude);
        }
        else
        {
            LocationLabel.Text = "Não foi possível obter a localização.";
        }
    }

    private void UpdateLocation(double latitude, double longitude)
    {
        _mapManager.UpdateLocation(latitude, longitude);
    }

    private bool _isTracking = false;

    private async void OnToggleTrackingClicked(object sender, EventArgs e)
    {
        if (!_isTracking)
        {
            await _locationService.StartListeningAsync();
            _isTracking = true;
            StartStopTrackingButton.Text = "🛑 Stop Running";
            StartStopTrackingButton.BackgroundColor = Colors.Red;
        }
        else
        {
            _mapManager.ResetProgress();
            _locationService.StopListening();
            _isTracking = false;
            StartStopTrackingButton.Text = "🏃 Start Running";
            StartStopTrackingButton.BackgroundColor = Colors.Green;
        }
    }


    private void OnMapControlSizeChanged(object? sender, EventArgs e)
    {
        if (_hasCenteredOnce) return;

        _mapManager.CenterMap(_latitude, _longitude);
        _hasCenteredOnce = true;
    }

    private void OnMapClicked(object? sender, MapInfoEventArgs e)
    {
        if (e.MapInfo?.WorldPosition == null) return;
        if(_isDrawingMode == false) return;

        var pos = e.MapInfo.WorldPosition;
        var (lon, lat) = SphericalMercator.ToLonLat(pos.X, pos.Y);

        _mapManager.AddVertex(lon, lat);
    }

    private void OnStartStopDrawingClicked(object sender, EventArgs e)
    {
        _isDrawingMode = !_isDrawingMode;

        if (_isDrawingMode)
        {
            StartDrawingButton.Text = "🛑 Stop Drawing";
            StartDrawingButton.BackgroundColor= Colors.Red;
            //StopDrawingButton.IsEnabled = true;
        }
        else
        {
            StartDrawingButton.Text = "✏️ Draw Route";
            StartDrawingButton.BackgroundColor = Colors.Blue;
        }
    }


    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Verifica se há pelo menos 2 pontos na rota
        if (_mapManager.VertexCount < 2)
        {
            await DisplayAlert("Rota Inválida", "Adicione pelo menos 2 pontos antes de salvar a rota.", "OK");
            return;
        }

        // Exibe o prompt para o nome da rota
        string routeName = await DisplayPromptAsync(
            "Salvar Rota",
            "Digite o nome da rota:",
            "Salvar",
            "Cancelar",
            "Nome da rota",
            maxLength: 50,
            keyboard: Keyboard.Text);

        // Verifica se o nome da rota é válido
        if (string.IsNullOrWhiteSpace(routeName))
        {
            await DisplayAlert("Cancelado", "A rota não foi salva.", "OK");
            return;
        }

        try
        {
            // Obtém os vértices e salva a rota
            var vertices = _mapManager.GetVertices().ToList();
            await _routeRepo.AddRouteAsync(routeName, vertices);

            // Exibe uma mensagem de sucesso
            await DisplayAlert("Sucesso", $"Rota \"{routeName}\" salva com sucesso!", "OK");
        }
        catch (Exception ex)
        {
            // Exibe mensagem de erro em caso de falha
            await DisplayAlert("Erro", $"Erro ao salvar a rota: {ex.Message}", "OK");
        }

        // Finaliza o modo de desenho e atualiza a UI
        _isDrawingMode = false;
        StartDrawingButton.Text = "Desenhar Rota";
        //StopDrawingButton.IsEnabled = false;

        // Limpa os vértices após salvar
        _mapManager.ClearVertices();
    }

    public void SetCenter(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
    }

    private async void ShowRoutesButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var routes = await _routeRepo.GetAllRoutesAsync();

            if (routes.Count == 0)
            {
                await DisplayAlert("Sem Rotas", "Não há rotas salvas.", "OK");
                return;
            }

            // Cria uma string com as rotas e seus vértices para exibir no pop-up
            string routeDetails = "";

            foreach (var route in routes)
            {
                routeDetails += $"Rota: {route.Name}\n";
                var vertices = await _routeRepo.GetVerticesByRouteIdAsync(route.Id);
                foreach (var vertex in vertices)
                {
                    routeDetails += $"  Vértice: Longitude: {vertex.lon}, Latitude: {vertex.lat}\n";
                }
                routeDetails += "\n";
            }

            // Exibe os detalhes no pop-up
            await DisplayAlert("Rotas Salvas", routeDetails, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Erro ao exibir as rotas: {ex.Message}", "OK");
        }
    }

    private async void OnChargeRoutesClicked(object sender, EventArgs e)
    {
        _mapManager.ResetProgress();

        await Navigation.PushAsync(new ChargeRoutes(this));
    }

    public async Task DrawSavedRouteAsync(int routeId)
    {
        var vertices = await _routeRepo.GetVerticesByRouteIdAsync(routeId);

        if (vertices == null || vertices.Count < 2)
        {
            await DisplayAlert("Erro", "Rota inválida ou vazia.", "OK");
            return;
        }

        _mapManager.ClearVertices();

        foreach (var vertex in vertices)
        {
            _mapManager.AddVertex(vertex.lon, vertex.lat);
        }

        var first = vertices.First();
        _mapManager.CenterMap(first.lat, first.lon);
    }

    private void OnRemoveLastPointClicked(object sender, EventArgs e)
    {
        _mapManager.RemoveLastVertex();
    }

}