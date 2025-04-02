namespace ABM.Models
{
    public class FiltroFirmasViewModel
    {
        public int Mes { get; set; } 
        public int Anio { get; set; } 

        public FiltroFirmasViewModel()
        {
            Mes = DateTime.Now.Month;
            Anio = DateTime.Now.Year;
        }
    }
}
