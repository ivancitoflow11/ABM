namespace ABM.Models
{
    public class KpiResultado
    {
        public string pais { get; set; }
        public string negocio { get; set; }
        public int sistemas_mes_actual { get; set; }
        public int sistemas_mes_anterior { get; set; }
        public int finiquitados_mes_actual { get; set; }
        public decimal p_cumplimiento_mes_actual { get; set; }
        public int finiquitados_mes_anterior { get; set; }
        public decimal p_cumplimiento_mes_anteior { get; set; }
        public int total_cuentas { get; set; }
    }
}
