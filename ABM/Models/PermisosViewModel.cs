namespace ABM.Models
{
    public class PermisosViewModel
    {
        public IEnumerable<PermisosSubMenu> PermisosSubMenu { get; set; }
        public IEnumerable<PermisosMenu> PermisosMenu { get; set; }
        public IEnumerable<Menu> Menu { get; set; }
        public IEnumerable<SubMenu> Submenu { get; set; }
    }
}
