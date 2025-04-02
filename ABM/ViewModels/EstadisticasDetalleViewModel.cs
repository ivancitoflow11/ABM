using ABM.Models;

namespace ABM.ViewModels
{
    public class EstadisticasDetalleViewModel
    {
        public IEnumerable<EstadisticasUsuarios> EstadisticasUsuarios { get; set; }
        public IEnumerable<DetalleAlertaSistema> DetalleAlertaSistema { get; set; }
    }
}