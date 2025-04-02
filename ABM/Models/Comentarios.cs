namespace ABM.Models
{
    public class Comentarios
    {
        public int idCarga { get; set; }
        public int idUsuario { get; set; }
        public int idRol { get; set; }
        public string Responsable { get; set; }
        public int idMotivo { get; set; }
        public string estado { get; set; }
        public string comentario { get; set; }
        public string evidencia { get; set; }
        public DateTime? fecha_autorizacion { get; set; }
        public DateTime? fecha_creacion { get; set; }
        public string llave_ex { get; set; }
        public string motivo { get; set; }
        public string nombreusuario { get; set; }

        public string fecha_autorizacion_format => (fecha_autorizacion != null) ? Convert.ToDateTime(fecha_autorizacion).ToString("dd-MM-yyyy") : "";
        public string fecha_creacion_format => (fecha_creacion != null) ? Convert.ToDateTime(fecha_creacion).ToString("dd-MM-yyyy") : "";
        public string perfil { get; set; }
        public string cargospr { get; set; }
        public string Nom_Gerencia { get; set; }
    }
}
