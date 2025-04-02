namespace ABM.Models
{
    public class GestionViewModel : ResumenGestionViewModel
    {
        public IEnumerable<CasosCargoViewModel> ListaCasosCargo { get; set; }
        public IEnumerable<EvidenciasFiniquitadoViewModel> ListaEvidenciasFiniquitado { get; set; }
        public IEnumerable<TendenciaDiariaViewModel> ListaTendenciaDiaria { get; set; }
        public IEnumerable<ResumenPaisViewModel> ListaResumenPais { get; set; }
    }
}
