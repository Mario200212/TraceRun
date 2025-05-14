using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Projections;
using Mapsui.Nts;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;
using Mapsui.Tiling;

namespace TraceRunApp;

public class MapManager
{
    private readonly MapControl _mapView;

    private readonly MemoryLayer _vertexLayer;
    private readonly MemoryLayer _lineLayer;
    private readonly MemoryLayer _locationPinLayer;

    private readonly List<IFeature> _vertexFeatures = new();
    private readonly List<IFeature> _lineFeatures = new();
    private readonly List<IFeature> _locationPinFeatures = new();

    private readonly List<(double Longitude, double Latitude)> _vertices = new(); // Explicit tuple element names

    public MapManager(MapControl mapView)
    {
        _mapView = mapView;

        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _vertexLayer = new MemoryLayer { Name = "Vertices", Features = _vertexFeatures };
        _lineLayer = new MemoryLayer { Name = "Lines", Features = _lineFeatures };
        _locationPinLayer = new MemoryLayer { Name = "LocationPin", Features = _locationPinFeatures };

        map.Layers.Add(_vertexLayer);
        map.Layers.Add(_lineLayer);
        map.Layers.Add(_locationPinLayer);

        _mapView.Map = map;
    }

    public void CenterMap(double latitude, double longitude, int zoomLevel = 15)
    {
        var center = SphericalMercator.FromLonLat(longitude, latitude).ToMPoint();
        _mapView.Map?.Navigator.CenterOnAndZoomTo(center, _mapView.Map.Navigator.Resolutions[zoomLevel]);
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        var center = SphericalMercator.FromLonLat(longitude, latitude).ToMPoint();
        _mapView.Map?.Navigator.CenterOn(center);
        DrawLocationPin(longitude, latitude);
    }

    public void DrawLocationPin(double lon, double lat)
    {
        var spherical = SphericalMercator.FromLonLat(lon, lat);
        var point = new NetTopologySuite.Geometries.Point(spherical.Item1, spherical.Item2);
        var feature = new GeometryFeature { Geometry = point };

        feature.Styles.Add(new SymbolStyle
        {
            SymbolScale = 0.5,
            Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Green),
            Outline = new Pen(Mapsui.Styles.Color.White, 2),
            SymbolType = SymbolType.Ellipse
        });

        _locationPinFeatures.Clear();
        _locationPinFeatures.Add(feature);
        _mapView.Refresh();
    }

    public void AddVertex(double lon, double lat)
    {
        _vertices.Add((Longitude: lon, Latitude: lat)); // Explicit tuple element names
        DrawVertex(lon, lat);
        DrawLines();
    }

    private void DrawVertex(double lon, double lat)
    {
        var point = SphericalMercator.FromLonLat(lon, lat);
        var geometry = new NetTopologySuite.Geometries.Point(point.Item1, point.Item2);

        var feature = new GeometryFeature { Geometry = geometry };
        feature.Styles.Add(new SymbolStyle
        {
            Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
            SymbolScale = 0.3,
            Outline = new Pen(Mapsui.Styles.Color.White, 2)
        });

        _vertexFeatures.Add(feature);
        _mapView.Refresh();
    }

    private void DrawLines()
    {
        _lineFeatures.Clear();

        for (int i = 0; i < _vertices.Count - 1; i++)
        {
            var start = SphericalMercator.FromLonLat(_vertices[i].Longitude, _vertices[i].Latitude).ToMPoint();
            var end = SphericalMercator.FromLonLat(_vertices[i + 1].Longitude, _vertices[i + 1].Latitude).ToMPoint();

            var line = new LineString(new[] {
            new Coordinate(start.X, start.Y),
            new Coordinate(end.X, end.Y)
        });

            var feature = new GeometryFeature { Geometry = line };
            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.Blue, 3)
            });

            _lineFeatures.Add(feature);
        }

        _mapView.Refresh();
    }

    public void UpdateSegmentHighlight(double userLat, double userLon)
    {
        if (_vertices.Count < 2 || _lineFeatures.Count != _vertices.Count - 1)
            return;

        var userCoord = SphericalMercator.FromLonLat(userLon, userLat).ToMPoint();

        // Encontra o ponto mais próximo
        int closestIndex = -1;
        double minDistance = double.MaxValue;

        for (int i = 0; i < _vertices.Count; i++)
        {
            var point = SphericalMercator.FromLonLat(_vertices[i].Longitude, _vertices[i].Latitude).ToMPoint();
            var distance = Math.Sqrt(Math.Pow(point.X - userCoord.X, 2) + Math.Pow(point.Y - userCoord.Y, 2));

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        // Limite de distância em metros (ajustável)
        double proximityThreshold = 30;

        // Atualiza estilos das linhas
        for (int i = 0; i < _lineFeatures.Count; i++)
        {
            bool shouldHighlight = false;

            if (closestIndex > 0 && i == closestIndex - 1) shouldHighlight = true;     // anterior
            if (i == closestIndex) shouldHighlight = true;                             // atual
            if (i == closestIndex + 1) shouldHighlight = true;                         // próximo

            var style = new VectorStyle
            {
                Line = new Pen(shouldHighlight ? Mapsui.Styles.Color.Red : Mapsui.Styles.Color.Blue, 3)
            };

            _lineFeatures[i].Styles.Clear();
            _lineFeatures[i].Styles.Add(style);
        }

        _mapView.Refresh();
    }


    public void ClearVertices()
    {
        _vertices.Clear();
        _vertexFeatures.Clear();
        _lineFeatures.Clear();
        _mapView.Refresh();
    }
    public int VertexCount => _vertices.Count;
    public List<(double Longitude, double Latitude)> GetVertices()
    {
        return _vertices;
    }

    public void RemoveLastVertex()
    {
        if (_vertices.Count == 0)
            return;

        // Remove o último ponto da lista
        _vertices.RemoveAt(_vertices.Count - 1);
        _vertexFeatures.RemoveAt(_vertexFeatures.Count - 1);

        // Redesenha a linha com os pontos restantes
        _lineFeatures.Clear();

        if (_vertices.Count >= 2)
        {
            var points = _vertices
                .Select(v => SphericalMercator.FromLonLat(v.Longitude, v.Latitude).ToMPoint())
                .Select(p => new Coordinate(p.X, p.Y))
                .ToArray();

            var line = new LineString(points);
            var feature = new GeometryFeature { Geometry = line };

            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.Blue, 3)
            });

            _lineFeatures.Add(feature);
        }

        _mapView.Refresh();
    }

}
