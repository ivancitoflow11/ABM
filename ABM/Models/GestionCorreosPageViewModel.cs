namespace ABM.Models;
using System.Collections.Generic;

public class GestionCorreosPageViewModel
{
	public IEnumerable<EnvioCorreoDetalleViewModel> ListaCorreos { get; set; }
	public EnvioCorreoDetalleViewModel CorreoParaCrear { get; set; }

	public GestionCorreosPageViewModel()
	{
		ListaCorreos = new List<EnvioCorreoDetalleViewModel>();
		CorreoParaCrear = new EnvioCorreoDetalleViewModel();
	}
}