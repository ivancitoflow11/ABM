
namespace ABM.Models
{
    public class TiempoInactividad
    {
        public string pais { get; set; }
        public string negocio { get; set; }
        public string sistema { get; set; }
        public string rutdni { get; set; }
        public string dv { get; set; }
        public string nombreusuario { get; set; }
        public string userid { get; set; }
        public string fecultlogin { get; set; }
        public string estado { get; set; }
        public int DiasDesdeUltLogin { get; set; }
    }
}