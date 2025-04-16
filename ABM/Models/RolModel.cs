namespace ABM.Models
{
    public class RolModel
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; }
        public int? IdPais { get; set; }
        public int? IdNegocio { get; set; }
        public int? IdVistaInicio { get; set; }

        public string Vista { get; set; }
    }
}
