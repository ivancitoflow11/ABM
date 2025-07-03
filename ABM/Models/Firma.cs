namespace ABM.Models
{
    public class Firma
    {
        public int idFirma { get; set; }
        public int codUsuarioResponsable { get; set; }
        public DateTime fechaFirma { get; set; }
        public string fechaCarga { get; set; }
        public int codGerencia { get; set; }
        public string comentario { get; set; }

        public int? IdPais { get; set; }
        public int? IdNegocio { get; set; }
        public int? IdSistema { get; set; }

        public string NombreUsuarioResponsable { get; set; }
        public string NombrePais { get; set; }
        public string NombreNegocio { get; set; }
        public string NombreSistema { get; set; }
    }
}
