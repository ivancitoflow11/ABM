using System.Globalization;

namespace ABM.Models
{
    public class cl_sodimac_matriz_perfil
    {
        public string modulo { get; set; }
        public string pais { get; set; }
        public string sistema { get; set; }
        public int? idPaisNegocioSistema { get; set; }
        public string perfil { get; set; }
        public string Nomccosto { get; set; }
        public string rutdni { get; set; }
        public string nombreusuario { get; set; }
        public string nombreResponsable { get; set; }
        public string userid { get; set; }
        public string cargospr { get; set; }
        public string dv { get; set; }
        public string codccosto { get; set; }
        public string codccostospr { get; set; }
        public string Nom_Gerencia { get; set; }
		public string Nom_Subgerencia { get; set; }
		public string Gerencia { get; set; }
        public string Subgerencia { get; set; }
        public string Evidencia { get; set; }
        public string cargomatriz { get; set; }
        public string perfilmatriz { get; set; }
        public string feccarga { get; set; }
		public string FechaCargaFormateada => DateTime.ParseExact(feccarga, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy");
		public DateTime fechaFirma { get; set; }
		public string cta_duplicada { get; set; }
        public string EstadoMatrizPerfil { get; set; }
        public DateTime? UltimaFirmaMesActual { get; set; }
		public string CantidadUsuarios { get; set; }
		public string codPerfil { get; set; }
        public DateTime? Fcarga { get; set; }
        public string responsable { get; set; }
        public string cargoActivo { get; set; }
        public string cargo { get; set; }
        // Nueva propiedad para Estado Matriz perfil
        public int? idNivelCargo { get; set; }
        public int? idUsuario { get; set; }
        public string estado_matriz { get; set; }
        public string matriz { get; set; }
        public string usocargo { get; set; }
        public string tipocarga { get; set; }
        public string nivelcargo { get; set; }
        public bool Firmado { get; set; }
		public string comentario { get; set; }
		public string codGerencia { get; set; }
		public string FcargaString => (Fcarga != null) ? Convert.ToDateTime(Fcarga).ToString("dd-MM-yyyy") : "";

        public string firmaResponsable { get; set; }

	}
}