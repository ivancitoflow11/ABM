// En la carpeta Models/UltimaConexion.cs
namespace ABM.Models
{
    public class UltimaConexion
    {
        public string rutdni { get; set; }
        public string dv { get; set; }
        public string nombreusuario { get; set; }
        public string estado { get; set; }
        public string fecha_finiquito { get; set; } 
        public string ultima_conexion { get; set; } 
        public string Fecha_AD { get; set; }      
        public string pais { get; set; }
        public string negocio { get; set; }
        public string sistema { get; set; }
        public int idPaisNegocioSistema { get; set; }
        public int? ID_gerencia { get; set; }
        public string Nom_Gerencia { get; set; }
        public int? ID_Subgerencia { get; set; }
        public string Nom_Subgerencia { get; set; }
    }
}