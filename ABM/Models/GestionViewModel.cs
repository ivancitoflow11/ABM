namespace ABM.Models
{
    public class GestionViewModel : ResumenGestionViewModel
    {
        // Inicialización por defecto evita null
        public IEnumerable<CasosCargoViewModel> ListaCasosCargo { get; set; } = new List<CasosCargoViewModel>();
        public IEnumerable<EvidenciasFiniquitadoViewModel> ListaEvidenciasFiniquitado { get; set; } = new List<EvidenciasFiniquitadoViewModel>();
        public IEnumerable<TendenciaDiariaViewModel> ListaTendenciaDiaria { get; set; } = new List<TendenciaDiariaViewModel>();
        public IEnumerable<ResumenPaisViewModel> ListaResumenPais { get; set; } = new List<ResumenPaisViewModel>();
    }

}
