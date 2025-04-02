namespace ABM.Models
{
    public class ListaMatrizViewModel
    {
        public IEnumerable<cl_sodimac_matriz_perfil> ListaMatrizPerfil { get; set; }
        public IEnumerable<cl_sodimac_matriz_perfil> ResumenMatrizPerfil { get; set; }
		public IEnumerable<cl_sodimac_matriz_perfil> FirmasMatriz { get; set; }
	}
}