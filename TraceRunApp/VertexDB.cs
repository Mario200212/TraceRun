using SQLite;

namespace TraceRunApp
{
    [Table("VertexDB")]
    public class VertexDB
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int RouteId { get; set; } // Relacionamento com a rota

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int Order { get; set; } // Ordem do ponto na rota
    }
}
