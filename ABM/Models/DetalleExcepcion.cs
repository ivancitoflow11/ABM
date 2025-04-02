namespace ABM.Models
{
    public class DetalleExcepcion
    {
        public long idPaisNegocioSistema { get; set; }

        public long idCarga { get; set; }
        public string sistema { get; set; }
        public string nombreusuario { get; set; }
        public string userid { get; set; }
        public string rutdni { get; set; }
        public string cargospr { get; set; }
        public string perfil { get; set; }
        public string Nom_Gerencia { get; set; }
		public string Nom_Subgerencia { get; set; }

        public DateTime? fechaEsperada_ex { get; set; }
    }
}
