namespace ABM.Models
{
    public class DetalleAlertaSistema
    {
        public string idPais { get; set; }
        public string Negocio { get; set; }
        public string Sistema { get; set; }
        public int TotalUsuarios { get; set; }
        public int Activos { get; set; }
        public int Finiquitados { get; set; }
        public int NoEncontrados { get; set; }
        public int Recontratados { get; set; }
        public int Duplicados { get; set; }
        public int De1a3DiasSinGestion { get; set; }
        public int De4a6DiasSinGestion { get; set; }
        public int MasDe6DiasSinGestion { get; set; }
    }
}
