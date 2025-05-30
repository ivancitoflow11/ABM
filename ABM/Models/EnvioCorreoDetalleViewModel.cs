
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class EnvioCorreoDetalleViewModel
{
	public int IdCorreos { get; set; }

	// Campos para los Desplegables
	[Required(ErrorMessage = "Debe seleccionar un País.")]
	[Display(Name = "País")]
	public int IdPais { get; set; }

	[Required(ErrorMessage = "Debe seleccionar un Negocio.")]
	[Display(Name = "Negocio")]
	public int IdNegocio { get; set; }

	[Required(ErrorMessage = "Debe seleccionar un Sistema.")]
	[Display(Name = "Sistema")]
	public int IdSistema { get; set; }

	public int IdPaisNegocioSistema { get; set; } // Se llenará internamente

	// Campos de la tabla ftc_envio_correo_detalle
	[Display(Name = "OSI")]
	[StringLength(100)]
	public string? OSI { get; set; }

	[Display(Name = "Correo OSI")]
	[StringLength(100)]
	[EmailAddress(ErrorMessage = "El formato del correo OSI no es válido.")]
	public string? Correo_OSI { get; set; }

	[Display(Name = "Responsable")]
	[StringLength(100)]
	public string? Responsable { get; set; }

	[Display(Name = "Correo Responsable")]
	[StringLength(100)]
	[EmailAddress(ErrorMessage = "El formato del correo del Responsable no es válido.")]
	public string? Correo_Responsable { get; set; }

	[Display(Name = "Gerente")]
	[StringLength(100)]
	public string? Gerente { get; set; }

	[Display(Name = "Correo Gerente")]
	[StringLength(100)]
	[EmailAddress(ErrorMessage = "El formato del correo del Gerente no es válido.")]
	public string? Correo_Gerente { get; set; }

	[Display(Name = "Jefe")]
	[StringLength(100)]
	public string? Jefe { get; set; }

	[Display(Name = "Correo Jefe")]
	[StringLength(100)]
	[EmailAddress(ErrorMessage = "El formato del correo del Jefe no es válido.")]
	public string? Correo_Jefe { get; set; }

	[Display(Name = "Otros Correos")]
	[StringLength(100)] // Ajustar si es necesario
	public string? Otros_Correos { get; set; }

	// Para llenar los desplegables en la vista
	public IEnumerable<SelectListItem>? Paises { get; set; }
	public IEnumerable<SelectListItem>? Negocios { get; set; }
	public IEnumerable<SelectListItem>? Sistemas { get; set; }

	// Para mostrar nombres en la lista
	public string? NombrePais { get; set; }
	public string? NombreNegocio { get; set; }
	public string? NombreSistema { get; set; }
}