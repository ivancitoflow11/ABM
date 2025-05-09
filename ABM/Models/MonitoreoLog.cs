namespace ABM.Models
{
    public class MonitoreoLog
    {
        public string Responsable { get; set; }
        public string Tarea { get; set; }
        public string Tipo { get; set; }
        public string Descripcion { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
