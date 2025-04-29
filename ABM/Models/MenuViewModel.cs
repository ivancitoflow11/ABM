namespace ABM.Models
{
	public class MenuViewModel
	{
		public int? ID_Menu { get; set; }
		public string NOMBRE_MENU { get; set; }
		public string? ICONO { get; set; }
		public string? VISTA { get; set; }
		public string? CONTROLADOR { get; set; }

		public List<SubmenuViewModel> Submenus { get; set; } = new();
	}
}
