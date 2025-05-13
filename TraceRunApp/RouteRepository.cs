using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TraceRunApp
{
    public class RouteRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public RouteRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<RouteDB>().Wait();
            _db.CreateTableAsync<VertexDB>().Wait();
        }

        public async Task AddRouteAsync(string name, List<(double lon, double lat)> vertices)
        {
            var route = new RouteDB { Name = name };
            await _db.InsertAsync(route);

            for (int i = 0; i < vertices.Count; i++)
            {
                var (lon, lat) = vertices[i];
                var vertex = new VertexDB
                {
                    RouteId = route.Id,
                    Latitude = lat,
                    Longitude = lon,
                    Order = i
                };
                await _db.InsertAsync(vertex);
            }
        }

        public async Task<List<RouteDB>> GetAllRoutesAsync()
        {
            return await _db.Table<RouteDB>().ToListAsync();
        }

        public async Task<RouteDB?> GetRouteByIdAsync(int id)
        {
            return await _db.FindAsync<RouteDB>(id);
        }

        public async Task<List<(double lon, double lat)>> GetVerticesByRouteIdAsync(int routeId)
        {
            // Consultar todos os vértices da rota e ordenar por ordem
            var vertices = await _db.Table<VertexDB>()
                .Where(v => v.RouteId == routeId)
                .OrderBy(v => v.Order)
                .ToListAsync();

            // Converter os vértices para tuplas de longitude e latitude
            return vertices.Select(v => (v.Longitude, v.Latitude)).ToList();
        }

        public async Task DeleteRouteAsync(int id)
        {
            await _db.DeleteAsync<RouteDB>(id);

            // Deleta os vértices também
            var vertices = await _db.Table<VertexDB>().Where(v => v.RouteId == id).ToListAsync();
            foreach (var v in vertices)
                await _db.DeleteAsync<VertexDB>(v.Id);
        }
    }
}
