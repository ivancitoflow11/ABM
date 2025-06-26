namespace ABM.Models
{
    public class MatrizActivo
    {
        public string Pais { get; set; }
        public string Sistema { get; set; }
        public int IdPaisNegocioSistema { get; set; }
        public string Rutdni { get; set; }
        public string Dv { get; set; }
        public string NombreUsuario { get; set; }
        public string UserId { get; set; }
        public string CargoSpr { get; set; }
        public string Perfil { get; set; }
        public string CodCcosto { get; set; }
        public string CodCcostoSpr { get; set; }
        public string NomCcosto { get; set; }
        public string CargoMatriz { get; set; }
        public string PerfilMatriz { get; set; }
        public DateTime FecCarga { get; set; }
        public string Cta_Duplicada { get; set; }
        public int? ID_gerencia { get; set; }
        public string Nom_Gerencia { get; set; }
        public int? ID_Subgerencia { get; set; }
        public string Nom_Subgerencia { get; set; }
        public int? IdUsuario { get; set; }
    }
}
