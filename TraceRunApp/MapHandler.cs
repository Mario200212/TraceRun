using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui;
using NetTopologySuite.Geometries;
using System;

using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;


namespace TraceRunApp
{
    public class MapHandler
    {
        private readonly MapControl _mapView;

        private readonly List<(double lon, double lat)> _vertices = new();
        private MemoryLayer _vertexLayer = new() { Name = "Vertices" };
        private MemoryLayer _lineLayer = new() { Name = "Lines" };
        private MemoryLayer _locationPinLayer = new() { Name = "LocationPin" };

        private double _latitude = -22.817;
        private double _longitude = -47.0632;
        private bool _hasCenteredOnce = false;

        public MapHandler(MapControl mapView)
        {
            _mapView = mapView;

            var map = new Mapsui.Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            map.Layers.Add(_vertexLayer);
            map.Layers.Add(_lineLayer);
            map.Layers.Add(_locationPinLayer);

            _mapView.Map = map;
            _mapView.SizeChanged += OnMapControlSizeChanged;
            _mapView.Info += OnMapClicked;
        }

        public void SetCenter(double lat, double lon)
        {
            _latitude = lat;
            _longitude = lon;
        }

        public void UpdateLocation(double lat, double lon)
        {
            var center = SphericalMercator.FromLonLat(lon, lat).ToMPoint();
            _mapView.Map?.Navigator.CenterOn(center);
            DrawLocationPin(lon, lat);
        }

        public void DrawLocationPin(double lon, double lat)
        {
            var spherical = SphericalMercator.FromLonLat(lon, lat);
            var point = new NetTopologySuite.Geometries.Point(spherical.x, spherical.y); // Specify the namespace explicitly to resolve ambiguity
            var feature = new GeometryFeature { Geometry = point };

            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.5,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Green),
                Outline = new Pen(Mapsui.Styles.Color.White, 2),
                SymbolType = SymbolType.Ellipse
            });

            _locationPinLayer.Features = new List<IFeature> { feature };
            _mapView.Refresh();
        }

        private void OnMapClicked(object? sender, MapInfoEventArgs e)
        {
            if (e.MapInfo?.WorldPosition == null) return;

            var pos = e.MapInfo.WorldPosition;
            var (lon, lat) = SphericalMercator.ToLonLat(pos.X, pos.Y);

            _vertices.Add((lon, lat));
            DrawVertex(pos.X, pos.Y);
            DrawLines();
        }

        private void DrawVertex(double x, double y)
        {
            var point = new NetTopologySuite.Geometries.Point(x, y); // Specify the namespace explicitly to resolve ambiguity
            var feature = new GeometryFeature { Geometry = point };

            feature.Styles.Add(new SymbolStyle
            {
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
                SymbolScale = 0.3,
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            });

            _vertexLayer.Features = new List<IFeature> { feature };
            _mapView.Refresh();
        }

        private void DrawLines()
        {
            if (_vertices.Count < 2) return;

            var points = _vertices
                .Select(v => SphericalMercator.FromLonLat(v.lon, v.lat).ToMPoint())
                .Select(p => new Coordinate(p.X, p.Y))
                .ToArray();

            var line = new LineString(points);
            var feature = new GeometryFeature { Geometry = line };

            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.Blue, 3)
            });

            _lineLayer.Features = new List<IFeature> { feature };
            _mapView.Refresh();
        }

        private void OnMapControlSizeChanged(object? sender, EventArgs e)
        {
            if (_hasCenteredOnce || _mapView?.Map == null) return;

            var center = SphericalMercator.FromLonLat(_longitude, _latitude).ToMPoint();
            _mapView.Map.Navigator.CenterOnAndZoomTo(center, _mapView.Map.Navigator.Resolutions[15]);
            _hasCenteredOnce = true;
        }
    }
}