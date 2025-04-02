namespace ABM.Models
{
    public class VisionViewModel
    {
        public IEnumerable<RiesgoSistemaViewModel> ListaRiesgoSistema { get; set; }
        public IEnumerable<RiesgoPaisViewModel> ListaRiesgoPais { get; set; }
        public IEnumerable<EvolucionViewModel> ListaEvolucion { get; set; }
        public IEnumerable<Abm_Sistema?> ListaSistema { get; set; }
        public IEnumerable<TendenciaDiariaViewModel> ListaTendenciaDiaria { get; set; }
        public IEnumerable<TendenciaDiariaViewModel>? ListaTendenciaDiariaSistema { get; set; }
        public IEnumerable<RiesgoSistemaViewModel> RiesgoPerfil { get; set; }
    }
}
