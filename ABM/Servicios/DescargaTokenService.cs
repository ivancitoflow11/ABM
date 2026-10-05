using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace ABM.Servicios
{
    // Token de descarga: firmado por el servidor, atado al usuario y al recurso, de vigencia corta y un solo uso.
    public interface IDescargaTokenService
    {
        (string Token, string Id) Emitir(int idUsuario, string recurso);
        bool TryConsumir(string token, int idUsuario, string recurso, out string id);
    }

    public class DescargaTokenService : IDescargaTokenService
    {
        private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(2);
        private static readonly object Candado = new object();

        private readonly ITimeLimitedDataProtector _protector;
        private readonly IMemoryCache _cache;

        public DescargaTokenService(IDataProtectionProvider provider, IMemoryCache cache)
        {
            _protector = provider.CreateProtector("ABM.Descargas.Token.v1").ToTimeLimitedDataProtector();
            _cache = cache;
        }

        public (string Token, string Id) Emitir(int idUsuario, string recurso)
        {
            var id = Guid.NewGuid().ToString("N");
            var token = _protector.Protect($"{idUsuario}|{recurso}|{id}", Vigencia);
            return (token, id);
        }

        public bool TryConsumir(string token, int idUsuario, string recurso, out string id)
        {
            id = null;

            if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
            {
                return false;
            }

            string contenido;
            try
            {
                // Lanza CryptographicException si la firma no es válida o si el token ya venció.
                contenido = _protector.Unprotect(token);
            }
            catch (CryptographicException)
            {
                return false;
            }

            var partes = contenido.Split('|');
            if (partes.Length != 3 || partes[0] != idUsuario.ToString() || partes[1] != recurso)
            {
                return false;
            }

            var claveUso = "descarga-token:" + partes[2];
            lock (Candado)
            {
                if (_cache.TryGetValue(claveUso, out _))
                {
                    return false;
                }

                _cache.Set(claveUso, true, Vigencia + TimeSpan.FromMinutes(1));
            }

            id = partes[2];
            return true;
        }
    }
}
