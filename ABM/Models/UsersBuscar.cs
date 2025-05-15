namespace ABM.Models
{
    public class UsersBuscar
    {
        public string rutdni { get; set; }
        public string dv { get; set; }


        public string pais { get; set; }
        public string negocio { get; set; }
        public string sistema { get; set; }

        public string nombreusuario { get; set; }
        public string cargospr { get; set; }
        public string cargo { get; set; }
        public string perfil { get; set; }
        public string cargomatriz { get; set; }
        public string perfilmatriz { get; set; }
        public string estado { get; set; }

        public string ultima_conexion { get; set; }
        public string fecha_finiquito { get; set; }
        public string Fecha_AD { get; set; }
        public string fecultlogin { get; set; }   
        public string fecfiniq { get; set; }      
        public string fechaad { get; set; }       


        public string Nomccostospr { get; set; }
        public string codccostospr { get; set; }
        public string codccosto { get; set; }

        public string userid { get; set; }
        public string Empresa { get; set; }

   
        public int idPaisNegocioSistema { get; set; }
        public int? ID_gerencia { get; set; }
        public string Nom_Gerencia { get; set; }
        public int? ID_Subgerencia { get; set; }
        public string Nom_Subgerencia { get; set; }


        public int rn { get; set; }
    }
}
