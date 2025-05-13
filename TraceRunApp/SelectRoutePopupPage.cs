using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;
using System.Collections.Generic;

namespace TraceRunApp
{
    public class SelectRoutePopupPage : PopupPage
    {
        private readonly List<RouteDB> _routes;

        public event EventHandler<int> RouteSelected;

        public SelectRoutePopupPage(List<RouteDB> routes)
        {
            _routes = routes;

            var stackLayout = new StackLayout { Padding = 10 };

            // Adiciona um botão para cada rota
            foreach (var route in _routes)
            {
                var button = new Button
                {
                    Text = route.Name,
                    VerticalOptions = LayoutOptions.Start,
                };

                button.Clicked += (s, e) => OnRouteButtonClicked(route.Id);
                stackLayout.Children.Add(button);
            }

            Content = new ScrollView
            {
                Content = stackLayout
            };
        }

        // Evento quando uma rota é selecionada
        private void OnRouteButtonClicked(int routeId)
        {
            RouteSelected?.Invoke(this, routeId);
            Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); // Fecha o pop-up
        }
    }
}
