using Dapper;
using ABM.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net.Mail;
using System.Net;

namespace ABM.Servicios
{
    public interface IRepositorioMatrizDiaria
    {
        Task Actualizarcl_matriz_diariaPorExcepcion(Cl_matriz_diaria cl_matriz_diaria, DateTime FechaHasta, string NombreMotivo, string Observaciones);
        Task<Cl_matriz_diaria> Obtenercl_matriz_diariaPorIdCarga(int IdCarga);
        Task GuardarExcepcionConEnvioCorreo(Cl_matriz_diaria cl_matriz_diaria, string observaciones, string correoEnvio);
        Task<IEnumerable<Cl_matriz_diaria>> ObtenerExcepcionesNotificadas();


    }

    public class RepositorioMatrizDiaria : IRepositorioMatrizDiaria
    {
        private string connectionString;
        public RepositorioMatrizDiaria(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("CadenaSQL");
        }


        public async Task<Cl_matriz_diaria> Obtenercl_matriz_diariaPorIdCarga(int IdCarga)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryFirstOrDefaultAsync<Cl_matriz_diaria>(@"SELECT * FROM im_matriz_diaria
                where idCarga = @IdCarga", new { IdCarga });

            }
        }


        public async Task Actualizarcl_matriz_diariaPorExcepcion(Cl_matriz_diaria cl_matriz_diaria,
            DateTime FechaHasta, string NombreMotivo, string Observaciones)
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                string Tabla = "im_matriz_diaria";


                cl_matriz_diaria.estado_ex = "CERRADO";
                cl_matriz_diaria.fechaEsperaba_ex = FechaHasta;
                cl_matriz_diaria.fechaActual_ex = DateTime.Now;
                cl_matriz_diaria.motivo_ex = NombreMotivo;
                cl_matriz_diaria.fechaModAdmin_ex = DateTime.Now.ToString("yyyy-MM-dd");
                cl_matriz_diaria.comentario_ex = Observaciones;

                await dbdapper.ExecuteAsync(@"UPDATE "+Tabla+@" SET
                                estado_ex = @estado_ex,
                                fechaEsperaba_ex = @fechaEsperaba_ex,
                                fechaActual_ex = @fechaActual_ex,
                                motivo_ex = @motivo_ex,
                                fechaModAdmin_ex = @fechaModAdmin_ex,
                                comentario_ex = @comentario_ex
                                WHERE idCarga = @idCarga", cl_matriz_diaria);
            }


        }


		public async Task GuardarExcepcionConEnvioCorreo(Cl_matriz_diaria cl_matriz_diaria, string observaciones, string correoEnvio)
		{
			using (IDbConnection dbdapper = new SqlConnection(connectionString))
			{
				dbdapper.Open();
				using (var transaction = dbdapper.BeginTransaction())
				{
					try
					{
						string tablaMatrizDiaria = "im_matriz_diaria";
						string tablaEnvioExc = "im_envio_exc";

						// Actualizar im_matriz_diaria
						cl_matriz_diaria.fechaActual_ex = DateTime.Now;
						cl_matriz_diaria.fechaModAdmin_ex = DateTime.Now.ToString("yyyy-MM-dd");
						cl_matriz_diaria.comentario_ex = observaciones;

						await dbdapper.ExecuteAsync($@"
                    UPDATE {tablaMatrizDiaria} SET
                    fechaActual_ex = @fechaActual_ex,
                    fechaModAdmin_ex = @fechaModAdmin_ex,
                    comentario_ex = @comentario_ex
                    WHERE idCarga = @idCarga", cl_matriz_diaria, transaction);

						// Insertar en im_envio_exc
						cl_matriz_diaria.correo_envio = correoEnvio;
						await dbdapper.ExecuteAsync($@"
                    INSERT INTO {tablaEnvioExc} (correo_envio, codCarga)
                    VALUES (@correo_envio, @idCarga)", cl_matriz_diaria, transaction);

						transaction.Commit();
					}
					catch
					{
						transaction.Rollback();
						throw;
					}
				}
			}
		}


        //public async Task GuardarExcepcionConEnvioCorreo(Cl_matriz_diaria cl_matriz_diaria, string observaciones, string correoEnvio)
        //{
        //    using (IDbConnection dbdapper = new SqlConnection(connectionString))
        //    {
        //        dbdapper.Open();
        //        try
        //        {
        //            string tablaMatrizDiaria = "im_matriz_diaria";
        //            string tablaEnvioExc = "im_envio_exc";

        //            // Actualizar im_matriz_diaria
        //            cl_matriz_diaria.fechaActual_ex = DateTime.Now;
        //            cl_matriz_diaria.fechaModAdmin_ex = DateTime.Now.ToString("yyyy-MM-dd");
        //            cl_matriz_diaria.comentario_ex = observaciones;

        //            await dbdapper.ExecuteAsync($@"
        //            UPDATE {tablaMatrizDiaria} SET
        //            fechaActual_ex = @fechaActual_ex,
        //            fechaModAdmin_ex = @fechaModAdmin_ex,
        //            comentario_ex = @comentario_ex
        //            WHERE idCarga = @idCarga", cl_matriz_diaria);

        //            // Insertar en im_envio_exc
        //            cl_matriz_diaria.correo_envio = correoEnvio;
        //            await dbdapper.ExecuteAsync($@"
        //            INSERT INTO {tablaEnvioExc} (correo_envio, codCarga)
        //            VALUES (@correo_envio, @idCarga)", cl_matriz_diaria);

        //            // Enviar correo electrónico
        //            await EnviarCorreoExcepcion(cl_matriz_diaria, observaciones, correoEnvio);
        //        }
        //        catch (Exception ex)
        //        {
        //            // Loguear el error
        //            Console.WriteLine($"Error en GuardarExcepcionConEnvioCorreo: {ex.Message}");
        //            throw; // Re-lanzar la excepción para que sea manejada en el controlador
        //        }
        //    }
        //}


        //private async Task EnviarCorreoExcepcion(Cl_matriz_diaria matriz, string observaciones, string correoEnvio)
        //{
        //    try
        //    {
        //        using (SmtpClient cliente = new SmtpClient("smtp.gmail.com", 587))
        //        {
        //            cliente.EnableSsl = true;
        //            cliente.UseDefaultCredentials = false;
        //            cliente.Credentials = new NetworkCredential("emp.relacionadas@imperial.cl", "Imperial.2021");

        //            MailMessage mensaje = new MailMessage
        //            {
        //                From = new MailAddress("emp.relacionadas@imperial.cl", "Sistema ABM Imperial"),
        //                Subject = "Notificación de Excepciones",
        //                Priority = MailPriority.Normal,
        //                IsBodyHtml = true
        //            };

        //            mensaje.To.Add(correoEnvio);
        //            mensaje.Bcc.Add("diego.fernandez@bidata.cl");

        //            string htmlCompleto = @"<table width='100%' cellpadding='0' cellspacing='0'>
        //                    <tbody>
        //                        <tr>
        //                            <td style='width:100%;margin:0;padding:0;background-color:#ffffff' align='center'>
        //                                <table width='100%' cellpadding='0' cellspacing='0'>
        //                                    <tbody>
        //                                        <tr>
        //                                            <td style='text-align:center'>
        //                                                <a href='https://www.imperial.cl/' style='font-family:Arial,'Helvetica Neue,Helvetica,sans-serif;font-size:24px;font-weight:bold;color:#2f3133;text-decoration:none' target='_blank'>
        //                                                    <img width='291px' height='auto' src='cid:imagen'>
        //                                                </a>
        //                                            </td>
        //                                        </tr>
        //                                        <tr>
        //                                            <td style='width:100%;margin:0;padding:0;border-top:1px solid #edeff2;border-bottom:1px solid #edeff2;background-color:#fff' width='100%'>
        //                                                <table style='width:auto;max-width:570px;margin:0 auto;padding:0' align='center' width='570' cellpadding='0' cellspacing='0'>
        //                                                    <tbody>
        //                                                        <tr>
        //                                                            <td style='font-family:Arial,Helvetica Neue, Helvetica,sans-serif;padding:35px'>
        //                                                                <h1 style='margin-top:0;color:#063661;font-size:19px;font-weight:bold;text-align:left'>
        //                                                                    Notificación de Excepciones
        //                                                                </h1>
        //                                                                <p style='margin-top:0;color:#74787e;font-size:16px;line-height:1.5em'>
        //                                                                    Se ha registrado una nueva excepción en el sistema. A continuación, se detallan los datos de la excepción:
        //                                                                </p>
        //                                                                <table style='width:100%;border-collapse:collapse;margin-top:15px;margin-bottom:15px;'>
        //                                                                    <tr>
        //                                                                        <th style='text-align:left;padding:8px;background-color:#f2f2f2;border:1px solid #ddd;'>Campo</th>
        //                                                                        <th style='text-align:left;padding:8px;background-color:#f2f2f2;border:1px solid #ddd;'>Valor</th>
        //                                                                    </tr>
        //                                                                    <tr>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>ID Carga</td>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>{idCarga}</td>
        //                                                                    </tr>
        //                                                                    <tr>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>Usuario</td>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>{nombreusuario}</td>
        //                                                                    </tr>
        //                                                                    <tr>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>Rut/DNI</td>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>{rutdni}</td>
        //                                                                    </tr>
        //                                                                    <tr>
        //                                                                        <td style=' padding:8px;border:1px solid #ddd;'>Observaciones</td>
        //                                                                        <td style='padding:8px;border:1px solid #ddd;'>{observaciones}</td>
        //                                                                    </tr>
        //                                                                </table>
        //                                                                <p style='margin-top:0;font-size:16px;line-height:1.5em'>
        //                                                                    Saludos,<br><strong style='color: #ff464b;'>SISTEMA ABM IMPERIAL</strong>
        //                                                                </p>
        //                                                            </td>
        //                                                        </tr>
        //                                                    </tbody>
        //                                                </table>
        //                                            </td>
        //                                        </tr>
        //                                        <tr>
        //                                            <td style='background-color:#f5f5f5'>
        //                                                <table style='width:auto;max-width:570px;margin:0 auto;padding:0;text-align:center' align='center' width='570' cellpadding='0' cellspacing='0'>
        //                                                    <tbody>
        //                                                        <tr>
        //                                                            <td style='font-family:Arial,'Helvetica Neue,Helvetica,sans-serif;color:#aeaeae;padding:20px 20px 20px 5px;text-align:center'>
        //                                                                <p style='margin-top:0;margin-bottom:0;color:#74787e;font-size:12px;line-height:1.5em'>
        //                                                                    Los contenidos de este correo son propiedad de sus respectivos autores.
        //                                                                </p>
        //                                                                <br>
        //                                                                <p style='margin-top:0;margin-bottom:0;color:#74787e;font-size:12px;line-height:1.5em'>
        //                                                                    Correo enviado de forma automática, favor no responder.
        //                                                                    <br>
        //                                                                    <a style='color:#040404' href='' target='_blank'>Sistema ABM Imperial</a> | by <a href='http://bidata.cl'>Bidata</a>
        //                                                                </p>
        //                                                            </td>
        //                                                        </tr>
        //                                                    </tbody>
        //                                                </table>
        //                                            </td>
        //                                        </tr>
        //                                    </tbody>
        //                                </table>
        //                            </td>
        //                        </tr>
        //                    </tbody>
        //                </table>";

        //            htmlCompleto = htmlCompleto.Replace("{idCarga}", matriz.idCarga.ToString())
        //                                       .Replace("{nombreusuario}", matriz.nombreusuario ?? "N/A")
        //                                       .Replace("{rutdni}", matriz.rutdni ?? "N/A")
        //                                       .Replace("{observaciones}", observaciones);

        //            mensaje.Body = htmlCompleto;

        //            await cliente.SendMailAsync(mensaje);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // se loguea el error
        //        Console.WriteLine($"Error al enviar correo: {ex.Message}");
        //        throw; // Esto re-lanza la excepción para que sea manejada en el nivel superior
        //    }
        //}

        public async Task<IEnumerable<Cl_matriz_diaria>> ObtenerExcepcionesNotificadas()
        {
            using (IDbConnection dbdapper = new SqlConnection(connectionString))
            {
                return await dbdapper.QueryAsync<Cl_matriz_diaria>(@"
            SELECT m.idCarga, 
                   e.correo_envio, 
                   m.comentario_ex, 
                   m.estado_ex, 
                   m.motivo_ex, 
                   m.usersSistaAdmin_ex, 
                   m.fechaModAdmin_ex, 
                   m.userSistRevisor_ex, 
                   m.fechaModRevisor_ex, 
                   m.dias_ex, 
                   m.aprobado_ex, 
                   m.llave_ex
            FROM im_matriz_diaria m
            INNER JOIN im_envio_exc e ON m.idCarga = e.codCarga
            WHERE e.correo_envio IS NOT NULL
            ORDER BY m.fechaActual_ex DESC");
            }
        }
    }
}
