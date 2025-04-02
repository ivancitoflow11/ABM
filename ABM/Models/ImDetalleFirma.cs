using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
    public class ImDetalleFirma
    {
        public int IdDetalleFirma { get; set; }
        public int CodFirma { get; set; }
        public string pais { get; set; }
        public string sistema { get; set; }
        public int? idPaisNegocioSistema { get; set; }
        public string rutdni { get; set; }
        public string dv { get; set; }
        public string nombreusuario { get; set; }
        public string userid { get; set; }
        public string cargospr { get; set; }
        public string perfil { get; set; }
        public string codccosto { get; set; }
        public string codccostospr { get; set; }
        public string Nomccosto { get; set; }
        public string cargomatriz { get; set; }
        public string perfilmatriz { get; set; }
        public string feccarga { get; set; }
        public string cta_duplicada { get; set; }
        public string Evidencia { get; set; }
    }

}
