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
    }
}
