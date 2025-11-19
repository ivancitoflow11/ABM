namespace ABM.ViewModels
{
    using ABM.Models;
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    public class BusquedaMasivaViewModel
    {
        public IFormFile ArchivoExcel { get; set; }
        public int? IdBusqueda { get; set; }
        public List<ResultadoBusquedaMasiva> Resultados { get; set; }
        public int TotalRegistros { get; set; }
        public int RegistrosConProblemas { get; set; }
        public string Estado { get; set; }
    }

    public class ResultadoBusquedaMasiva
    {
        public string InputBusqueda { get; set; }
        public string Rut { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string EstadoAD { get; set; }
        public string UltimoLoginAD { get; set; }
        public bool EsFiniquitado { get; set; }
        public DateTime? FechaFiniquito { get; set; }
        public bool SprActivo { get; set; }
        public bool EmpCentralActivo { get; set; }
        public List<string> Negocios { get; set; }
        public List<SistemaUsuario> Sistemas { get; set; }
        public bool TieneProblemas { get; set; }
    }
}