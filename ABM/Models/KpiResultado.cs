using System;

namespace ABM.Models
{
    public class KpiResultado
    {
        public DateTime fecha_carga { get; set; }
        public int mes { get; set; }
        public int año { get; set; }
        public string PAIS { get; set; }
        public int n_finiquitados { get; set; }
        public int n_finiquitados_total { get; set; }
        public string cumplimiento_finiquitados { get; set; } 
        public int n_60 { get; set; }
        public int n_60_total { get; set; }
        public string cumplimiento_60 { get; set; }
        public int n_180 { get; set; }
        public int n_180_total { get; set; }
        public string cumplimiento_180 { get; set; }
        public int n_generica_sin_responsable { get; set; } 
        public int n_generica_sin_responsable_total { get; set; } 
        public string cumplimiento_generica_sin_responsable { get; set; } 
        public int n_usuarios_no_logeados { get; set; }
        public int n_usuarios_no_logeados_total { get; set; } 
        public string cumplimiento_usuarios_no_logeados { get; set; } 
        public int usuarios_duplicados { get; set; }
        public int n_usuarios_duplicados { get; set; }
        public int n_usuarios_duplicados_total { get; set; } 
        public string cumplimiento_usuarios_duplicados { get; set; } 
        public int n_usuarios_pass_no_expira { get; set; }
        public int n_usuarios_pass_no_expira_total { get; set; } 
        public string cumplimiento_usuarios_pass_no_expira { get; set; } 
        public int n_genericos_pass_no_expira { get; set; } 
        public int n_genericos_pass_no_expira_total { get; set; } 
        public string cumplimiento_genericos_pass_no_expira { get; set; } 
    }
}
