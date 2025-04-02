namespace ABM.Models
{
    public class RiesgoSistemaViewModel
    {
        public string Sistema { get; set; }
        public string pais { get; set; }
        public int Activos { get; set; }
        public int nriesgo { get; set; }
        public int Riesgo_Alto { get; set; }
        public int Riesgo_Medio { get; set; }
        public int Riesgo_Bajo { get; set; }
        public int Finiquitados { get; set; }
        public int NoEncontrados { get; set; }  
    }
}
