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
        public int idPais { get; set; }
        public string negocio { get; set; }
        public int idNegocio { get; set; }
        public List<string> sistemas { get; set; }
    }

}
