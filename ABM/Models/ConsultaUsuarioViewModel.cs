namespace ABM.ViewModels
{
    using ABM.Models;
    using System.Linq;

    public class ConsultaUsuarioViewModel
    {
        public string Input { get; set; }
        public string UltimoLoginAD { get; set; }
        public DatosBasicosUsuario Usuario { get; set; }
        public string EstadoAD { get; set; }
        public bool EsFiniquitado { get; set; }
        public bool SprActivo { get; set; }
        public bool EmpCentralActivo { get; set; }
        public DateTime? FechaFiniquito { get; set; }
        public IEnumerable<SistemaUsuario> Sistemas { get; set; }

        // 👇 NUEVA propiedad calculada para obtener negocios únicos
        public IEnumerable<string> NegociosUnicos
        {
            get
            {
                if (Sistemas == null || !Sistemas.Any())
                    return Enumerable.Empty<string>();

                return Sistemas
                    .Where(s => !string.IsNullOrWhiteSpace(s.NegocioPais))
                    .Select(s => s.NegocioPais)
                    .Distinct()
                    .OrderBy(n => n);
            }
        }
    }
}