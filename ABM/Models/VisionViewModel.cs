namespace ABM.Models
{
    public class VisionViewModel
    {
        public IEnumerable<PaisViewModel> ListaPaises { get; set; } = new List<PaisViewModel>();
        public int SelectedPais { get; set; }

        public IEnumerable<EvolucionViewModel> ListaEvolucion { get; set; } = new List<EvolucionViewModel>();
        public IEnumerable<RiesgoSistemaViewModel> ListaRiesgoSistema { get; set; } = new List<RiesgoSistemaViewModel>();
        public IEnumerable<RiesgoPaisViewModel> ListaRiesgoPais { get; set; } = new List<RiesgoPaisViewModel>();
        public IEnumerable<Abm_Sistema> ListaSistema { get; set; } = new List<Abm_Sistema>();
        public IEnumerable<TendenciaDiariaViewModel> ListaTendenciaDiaria { get; set; } = new List<TendenciaDiariaViewModel>();
        public IEnumerable<TendenciaDiariaViewModel> ListaTendenciaDiariaSistema { get; set; } = new List<TendenciaDiariaViewModel>();
        public IEnumerable<RiesgoSistemaViewModel> RiesgoPerfil { get; set; } = new List<RiesgoSistemaViewModel>();
        public IEnumerable<RiesgoPaisViewModel> ListaRiesgoGlobalMapa { get; set; } = new List<RiesgoPaisViewModel>();
    }
}
