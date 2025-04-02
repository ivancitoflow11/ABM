using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABM.Models
{
	public class ImFirma
	{
		[Key]
		public int IdFirma { get; set; }
		public int CodUsuarioResponsable { get; set; }
		public DateTime FechaFirma { get; set; }
		[Column("fechaCarga")]  // Actualizado para coincidir con la columna de la base de datos
		public string FechaCarga { get; set; }
		public int? CodGerencia { get; set; }
		[MaxLength(200)]
		public string Comentario { get; set; }
		// Relaciones
		public virtual ICollection<ImDetalleFirma> DetalleFirmas { get; set; }
        public string NombreUsuario { get; set; }
        public string Nom_Gerencia { get; set; }
        public string FirmaBase64 { get; set; }
    }

}
