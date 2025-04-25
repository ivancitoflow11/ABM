namespace ABM.Models
{
    public class ReportesViewModel
    {
        public IEnumerable<Abm_Sistema> ListaSistemas { get; set; }
        public IEnumerable<TiempoInactividad> ListaFiltroInactividad { get; set; }
        public string sistema { get; set; }
        public int? idSistema { get; set; }
        public int? idpais { get; set; }
        public int? idnegocio { get; set; }
        public IEnumerable<Finiquitados> ListaFiltroFiniquitados { get; set; }
        public IEnumerable<UsuariosNoEncontrados> ListaUsuariosNoEncontrados { get; set; }
        public IEnumerable<UsuariosActivos> ListaUsuariosActivos { get; set; }
        public IEnumerable<DifCargoPerfil> ListaDifCargoPerfil { get; set; }
        public IEnumerable<UltimaConexion> ListaFiltroUltimaConexion { get; set; }
        public string nombreusuario { get; set; }
        public string rutdni { get; set; }
        public IEnumerable<UsersBuscar> ListaBuscarUsuario { get; set; }
        public IEnumerable<InfoUser> ListaInfoUsuario { get; set; }
    }
}
