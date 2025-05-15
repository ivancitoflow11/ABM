namespace ABM.Models
{
    public class HomeViewModel
    {
        public bool IsAuthenticated { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public string RolNombre { get; set; }
        public string Pais { get; set; }
        public string Negocio { get; set; }
    }
}
