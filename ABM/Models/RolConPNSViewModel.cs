namespace ABM.Models
{
    public class RolConPNSViewModel
    {
        public int idRol { get; set; }
        public string nombreRol { get; set; }
        public List<PaisNegocioViewModel> PaisesNegocios { get; set; } = new();
    }

    public class PaisNegocioViewModel
    {
        public string pais { get; set; }
        public string negocio { get; set; }
        public List<string> sistemas { get; set; } = new();
    }

}
