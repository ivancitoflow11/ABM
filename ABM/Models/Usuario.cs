namespace ABM.Models
{
    public class Usuario
    {
        public int idUsuario { get; set; }
        public string nombre { get; set; }
        public string apellidos { get; set; }
        public string rut { get; set; }
        public string telefono { get; set; }
        public string correo { get; set; }
        public string usuario { get; set; }
        public string password { get; set; }
        public string repeat_password { get; set; }
        public DateTime? FultimoAcceso { get; set; }
        public DateTime? FultimaModificacion { get; set; }
        public DateTime? Fcreacion { get; set; }
        public string estado { get; set; }

        public int? idRol { get; set; }
        public bool estado_password { get; set; }
        public string session { get; set; }
        public string otc { get; set; }
        public DateTime? inicioOtc { get; set; }
        public int MesesExpiracionClave { get; set; }
        public DateTime? FechaCambioPassword { get; set; }
        public byte[] firma { get; set; }
        public bool? ResponsableFirma { get; set; }
        public bool primerInicio { get; set; }

    }
}
