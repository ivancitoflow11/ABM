namespace ABM.ViewModels
{
    using ABM.Models;

    public class ConsultaUsuarioViewModel
    {
        public string Input { get; set; }
        public DatosBasicosUsuario Usuario { get; set; }
        public string EstadoAD { get; set; }
        public bool EsFiniquitado { get; set; }
        public bool SprActivo { get; set; }
        public bool EmpCentralActivo { get; set; }
    }
}
