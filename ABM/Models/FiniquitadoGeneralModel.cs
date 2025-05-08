namespace ABM.Models
{
    public class FiniquitadoGeneralModel
    {
        public int idFiniquitados_cl { get; set; }
        public string PAIS { get; set; }
        public string codigoempresa { get; set; }
        public double? rutempresa { get; set; }
        public string empresa { get; set; }
        public string codgrupo { get; set; }
        public string grupoempresa { get; set; }
        public string rutdni { get; set; }
        public string dv { get; set; }
        public double? codigoeempleado { get; set; }
        public string nombreusuario { get; set; }
        public double? codjefe { get; set; }
        public string fecinicontarto { get; set; }
        public string fecfiniquito { get; set; }
        public string causal { get; set; }
        public double? codccosto { get; set; }
        public string Nomccosto { get; set; }
        public double? codcargospr { get; set; }
        public string cargospr { get; set; }
        public string rolempleado { get; set; }
        public string tipoempleado { get; set; }
        public string feccarga { get; set; }
        public string tipocarga { get; set; }
        public string numuser { get; set; }
    }
}