namespace ABM.Models
{
	public class AuditoriaFirmas
	{
		public int ID_Gerencia { get; set; }
		public string Nom_Gerencia { get; set; }
		public string Sistema { get; set; }
		public string EstadoFirma { get; set; }
		public string ResponsableFirma { get; set; } 
		public DateTime? FechaFirma { get; set; }  
	}
}
