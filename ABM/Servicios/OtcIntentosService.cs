using Microsoft.Extensions.Caching.Memory;

namespace ABM.Servicios
{
    // Límite de intentos fallidos de validación de OTC y de frecuencia de reenvío del código.
    // El estado vive en memoria del proceso: un reinicio de la app lo reinicia, y el código sigue
    // acotado por su vigencia y por su único uso.
    public interface IOtcIntentosService
    {
        bool EstaBloqueado(string clave, int maxFallos);
        void RegistrarFallo(string clave);
        void Reiniciar(string clave);
        bool TryReservarEnvio(string clave);
    }

    public class OtcIntentosService : IOtcIntentosService
    {
        public static readonly TimeSpan Ventana = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan EsperaReenvio = TimeSpan.FromSeconds(30);

        private static readonly object Candado = new object();
        private readonly IMemoryCache _cache;

        public OtcIntentosService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private sealed class Contador
        {
            public int Fallos;
        }

        private static string ClaveFallos(string clave) => "otc-fallos:" + clave;

        public bool EstaBloqueado(string clave, int maxFallos)
        {
            return _cache.TryGetValue(ClaveFallos(clave), out Contador contador) && contador.Fallos >= maxFallos;
        }

        // La ventana corre desde el primer fallo; al vencer, el contador desaparece solo.
        public void RegistrarFallo(string clave)
        {
            lock (Candado)
            {
                var k = ClaveFallos(clave);
                if (!_cache.TryGetValue(k, out Contador contador))
                {
                    contador = new Contador();
                    _cache.Set(k, contador, Ventana);
                }

                contador.Fallos++;
            }
        }

        public void Reiniciar(string clave)
        {
            _cache.Remove(ClaveFallos(clave));
        }

        public bool TryReservarEnvio(string clave)
        {
            lock (Candado)
            {
                var k = "otc-envio:" + clave;
                if (_cache.TryGetValue(k, out _))
                {
                    return false;
                }

                _cache.Set(k, true, EsperaReenvio);
                return true;
            }
        }
    }
}
