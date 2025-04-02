namespace ABM.Models
{
    public class EvidenciasFiniquitadoViewModel
    {
        public string Sistema { get; set; }
        public int finiquitados_hoy { get; set; }
        public int entre_1_3 { get; set; }
        public int entre_4_6 { get; set; }
        public int mayor_a_6 { get; set; }
    }
}
