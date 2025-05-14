using System;

namespace ABM.Models
{
    public class Cl_matriz_diaria
    {
        public int idCarga { get; set; }
        public DateTime? fechaInicio_ex { get; set; } 
        public DateTime? fechaFin_ex { get; set; }   
        public string estado_ex { get; set; }      
        public DateTime? fechaActual_ex { get; set; } 
        public DateTime? fechaEsperaba_ex { get; set; } 
        public DateTime? fechaAutorizacion_ex { get; set; } 
        public string motivo_ex { get; set; }     
        public string usersSistaAdmin_ex { get; set; } 
        public DateTime? fechaModAdmin_ex { get; set; } 
        public string userSistRevisor_ex { get; set; } 
        public DateTime? fechaModRevisor_ex { get; set; } 
        public int? dias_ex { get; set; }            
        public string aprobado_ex { get; set; }     
        public string llave_ex { get; set; }        
        public string fechaesperada_ex { get; set; }  
        public int? idPaisNegocioSistema { get; set; } 
        public int? idSemaforo { get; set; }        
        public string comentario_ex { get; set; }    
        public int? idExcepcion { get; set; }       
        public string estadousuario { get; set; }    
        public string rutdni { get; set; }        
        public string nombreusuario { get; set; }   
        public string userid { get; set; }          
        public string cargospr { get; set; }      
        public string perfil { get; set; }    
        public string fecfiniq { get; set; }      
        public string ultimoliginad { get; set; }    
        public string cargoMatriz { get; set; }    
        public string perfilMatriz { get; set; }     
        public string Nomccostospr { get; set; }    
        public string fecultlogin { get; set; }     
        public string fecalta { get; set; }        
        public string fecbaja { get; set; }          
        public string codccosto { get; set; }       
        public string dv { get; set; }                

        public int idEnvio { get; set; }
        public string correo_envio { get; set; }
    }
}
