namespace ABM.Models
{
    public class Menu
    {
        public int ID_Menu { get; set; }
        public string DESC_NombreMenu { get; set; }
        public bool IsChecked { get; set; }
        public string? DESC_Controlador { get; set; }
        public int? OrdenMenu { get; set; }
        public string? DESC_Vista { get; set; }
        public IEnumerable<SubMenu>? ListaSubMenu { get; set; }
        public string DESC_Icono { get; set; }
        public int COD_VistaPrincipal { get; set; }
    }

}
