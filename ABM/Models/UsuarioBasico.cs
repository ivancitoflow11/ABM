namespace ABM.Models
{
    public class DatosBasicosUsuario
    {
        // --- Coincidencias obligatorias con el SP ---

        // El SP devuelve 'nombreusuario', así que la propiedad debe llamarse igual
        public string NombreUsuario { get; set; }

        // El SP devuelve 'mailusuario'
        public string MailUsuario { get; set; }

        // El SP devuelve 'rutdni'
        public string RutDni { get; set; }

        // --- Nuevos datos que tu SP ahora devuelve y podrías aprovechar ---
        public string Dv { get; set; }
        public string CargoSpr { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; }
        public string Negocio { get; set; }
        public string Sistema { get; set; }
        public string NegocioPais { get; set; }
    }
}