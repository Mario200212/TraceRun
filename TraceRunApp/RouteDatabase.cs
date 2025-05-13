// Services/RouteDatabase.cs
using SQLite;
using System.Text.Json;
using TraceRunApp.Models;

namespace TraceRunApp.Services;

public class RouteDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public RouteDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<RouteModel>().Wait();
    }

    public Task<List<RouteModel>> GetRoutesAsync() => _database.Table<RouteModel>().ToListAsync();

    public Task<int> SaveRouteAsync(string name, List<(double Longitude, double Latitude)> points)
    {
        var json = JsonSerializer.Serialize(points);
        var route = new RouteModel { Name = name, PointsJson = json };
        return _database.InsertAsync(route);
    }

    public Task<int> DeleteRouteAsync(RouteModel route) => _database.DeleteAsync(route);

    public List<(double Longitude, double Latitude)> DeserializePoints(string json)
    {
        return JsonSerializer.Deserialize<List<(double, double)>>(json)!
            .Select(p => (p.Item1, p.Item2)).ToList();
    }
}
