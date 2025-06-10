using System.ComponentModel.DataAnnotations;

namespace ABM.Models
{
	public class EnvioCorreoDetalle
	{
        [Key] 
        public int idDetalle { get; set; }
        public int IdCorreos { get; set; } 
		public int IdPaisNegocioSistema { get; set; } // Foreign Key

		[StringLength(100)]
		public string? OSI { get; set; }

		[StringLength(100)]
		[EmailAddress(ErrorMessage = "El formato del correo OSI no es válido.")]
		public string? Correo_OSI { get; set; }

		[StringLength(100)]
		public string? Responsable { get; set; }

		[StringLength(100)]
		[EmailAddress(ErrorMessage = "El formato del correo del Responsable no es válido.")]
		public string? Correo_Responsable { get; set; }

		[StringLength(100)]
		public string? Gerente { get; set; }

		[StringLength(100)]
		[EmailAddress(ErrorMessage = "El formato del correo del Gerente no es válido.")]
		public string? Correo_Gerente { get; set; }

		[StringLength(100)]
		public string? Jefe { get; set; }

		[StringLength(100)]
		[EmailAddress(ErrorMessage = "El formato del correo del Jefe no es válido.")]
		public string? Correo_Jefe { get; set; }

		[StringLength(100)] 
		public string? Otros_Correos { get; set; } 
	}
}
