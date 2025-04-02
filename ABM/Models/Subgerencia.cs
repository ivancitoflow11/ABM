namespace ABM.Models
{
    public class Subgerencia
    {
        public int ID_Subgerencia { get; set; }
        public string Nom_Subgerencia { get; set; }
        public int COD_Gerencia { get; set; }
        public Gerencia Gerencia { get; set; }
    }
}