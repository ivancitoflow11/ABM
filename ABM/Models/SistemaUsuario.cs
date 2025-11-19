namespace ABM.Models
{
    public class SistemaUsuario
    {
        public string rutdni { get; set; }
        public string dv { get; set; }
        public string nombreusuario { get; set; }
        public string mailusuario { get; set; }
        public string cargospr { get; set; }
        public string estado { get; set; }  // 👈 Asegúrate que esté con minúscula
        public string pais { get; set; }
        public string negocio { get; set; }
        public string sistema { get; set; }  // 👈 Asegúrate que esté con minúscula
        public string NegocioPais { get; set; }
    }
}