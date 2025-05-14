using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TraceRunApp
{
    public class LocationService
    {
        public event Action<Location> LocationUpdated;

        private CancellationTokenSource _trackingTokenSource;
        private bool _isTracking;


        private CancellationTokenSource _cancelTokenSource;
        private bool _isCheckingLocation;

        public async Task<Location> GetCurrentLocationAsync()
        {
            try
            {
                _isCheckingLocation = true;

                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                _cancelTokenSource = new CancellationTokenSource();

                var location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

                return location;
            }
            catch (FeatureNotSupportedException)
            {
                Console.WriteLine("GPS não suportado no dispositivo.");
            }
            catch (FeatureNotEnabledException)
            {
                Console.WriteLine("GPS está desativado.");
            }
            catch (PermissionException)
            {
                Console.WriteLine("Permissão de localização negada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
            finally
            {
                _isCheckingLocation = false;
            }

            return null;
        }

        public void CancelRequest()
        {
            if (_isCheckingLocation && _cancelTokenSource?.IsCancellationRequested == false)
                _cancelTokenSource.Cancel();
        }

        public async Task StartListeningAsync()
        {
            try
            {
                Geolocation.LocationChanged += OnLocationChanged;
                Geolocation.ListeningFailed += OnListeningFailed;

                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
                var success = await Geolocation.StartListeningForegroundAsync(request);

                Console.WriteLine(success
                    ? "Started listening for location updates"
                    : "Failed to start listening");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar rastreamento: {ex.Message}");
            }
        }

        public void StopListening()
        {
            try
            {
                Geolocation.LocationChanged -= OnLocationChanged;
                Geolocation.ListeningFailed -= OnListeningFailed;

                Geolocation.StopListeningForeground();
                Console.WriteLine("Parou o rastreamento");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao parar rastreamento: {ex.Message}");
            }
        }

        private void OnLocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            LocationUpdated?.Invoke(e.Location);
        }

        private void OnListeningFailed(object sender, GeolocationListeningFailedEventArgs e)
        {
            Console.WriteLine($"Erro de rastreamento: {e.Error}");
        }

    }
}
