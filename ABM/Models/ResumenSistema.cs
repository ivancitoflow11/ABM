namespace ABM.Models
{
    public class ResumenSistema
    {
        public string Sistema { get; set; }
        public int Finiquitado { get; set; }
        public int Activo { get; set; }
        public int No_Encontrado { get; set; } 
        public int Totales { get; set; }
        public DateTime Fecha_Reporte { get; set; }
        public DateTime? Fecha_Actualizacion { get; set; } 
    }
}