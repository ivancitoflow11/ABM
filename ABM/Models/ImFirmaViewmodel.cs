namespace ABM.Models
{
    public class ImFirmaViewmodel
    {
        public ImFirmaViewmodel()
        {
            Firmas = new List<ImFirma>();
            Detalles = new List<ImDetalleFirma>();
        }

        public List<ImFirma> Firmas { get; set; }
        public List<ImDetalleFirma> Detalles { get; set; }
    }

}
