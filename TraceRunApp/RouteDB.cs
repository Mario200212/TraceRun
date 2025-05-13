using SQLite;


namespace TraceRunApp
{
    [Table("RouteDB")]
    public class RouteDB
    {
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }

        [MaxLength(50),Unique]
        public string? Name { get; set; }
    }
}
