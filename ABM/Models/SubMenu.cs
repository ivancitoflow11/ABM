namespace ABM.Models
{
    public class SubMenu
    {
        public int ID_Submenu { get; set; }
        public string DESC_NombreMenu { get; set; }
        public bool IsChecked { get; set; }
        public string? DESC_Controlador { get; set; }
        public int? OrdenMenu { get; set; }
        public string? DESC_Vista { get; set; }
        public int? COD_Menu { get; set; }
    }
}
