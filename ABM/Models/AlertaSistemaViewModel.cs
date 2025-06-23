namespace ABM.Models
{
    public class AlertaSistemaViewModel
    {
        public IEnumerable<Finiquitados> ListaFiltroFiniquitados { get; set; }
        public IEnumerable<UsuariosNoEncontrados> ListaUsuariosNoEncontrados { get; set; }
        public IEnumerable<EstadisticasUsuarios> estadisticas { get; set; }
        public IEnumerable<UsuariosDuplicados> ListaUsuariosDuplicados { get; set; }

        public IEnumerable<IGrouping<dynamic, EstadisticasUsuarios>> EstadisticasAgrupadas { get; set; }

        public IEnumerable<PaisViewModel> PaisesEnCabecera { get; set; }
    }
}
