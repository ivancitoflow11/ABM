namespace ABM.Models
{
    public class FiniquitadoUser
    {
        public string? RutDni { get; set; }
        public string? Dv { get; set; }
        public string? NombreUsuario { get; set; }
        public string? MailUsuario { get; set; }
        public string? CargoSpr { get; set; }
        public string? Sistema { get; set; }
        public string? Pais { get; set; }
        public string? Negocio { get; set; }
        public string? FecFiniq { get; set; } // Fecha de Finiquito es clave aquí
        public string? NomGerencia { get; set; }
    }
}