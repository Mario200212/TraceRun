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
        if (_vertices.Count < 2) return;

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

        _lineFeatures.Clear();
        _lineFeatures.Add(feature);
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
}
