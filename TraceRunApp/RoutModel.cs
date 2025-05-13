// Models/RouteModel.cs
using SQLite;

namespace TraceRunApp.Models;

public class RouteModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string PointsJson { get; set; } = "";
}
