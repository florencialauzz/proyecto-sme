using System;
using System.Text;
using Sme.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Sme.Managers
{
    // Cliente HTTP genérico para hablar con el backend Spring Boot (contratos/api-contract.md).
    // No es un MonoBehaviour: UnityWebRequest se puede completar por callback sin
    // necesitar una corutina ni un GameObject que la hostee.
    public static class ApiClient
    {
        private const string BaseUrl = "http://localhost:8080/api";
        private const string MensajeErrorConexion = "Ocurrió un error de conexión. Intentá de nuevo más tarde.";

        public static void Post<TRequest, TResponse>(
            string ruta,
            TRequest cuerpo,
            Action<TResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            EnviarConCuerpo(UnityWebRequest.kHttpVerbPOST, ruta, cuerpo, alTenerExito, alFallar);
        }

        // RF-21: PUT /grilla reemplaza la grilla completa cada vez.
        public static void Put<TRequest, TResponse>(
            string ruta,
            TRequest cuerpo,
            Action<TResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            EnviarConCuerpo(UnityWebRequest.kHttpVerbPUT, ruta, cuerpo, alTenerExito, alFallar);
        }

        private static void EnviarConCuerpo<TRequest, TResponse>(
            string verbo,
            string ruta,
            TRequest cuerpo,
            Action<TResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            string json = JsonUtility.ToJson(cuerpo);
            byte[] cuerpoBytes = Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(BaseUrl + ruta, verbo)
            {
                uploadHandler = new UploadHandlerRaw(cuerpoBytes),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");

            // Endpoints protegidos (todos salvo /auth/registro y /auth/login) necesitan
            // el token de la sesión activa (contratos/api-contract.md, sección 1).
            if (SesionManager.HaySesionActiva)
            {
                request.SetRequestHeader("Authorization", "Bearer " + SesionManager.Token);
            }

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    TResponse respuesta = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                    alTenerExito(respuesta);
                }
                else
                {
                    NotificarError(request.downloadHandler.text, alFallar);
                }

                request.Dispose();
            };
        }

        // RF-22: GET /proyectos. Método dedicado (no un Get<T> genérico) porque
        // JsonUtility no puede parsear un array JSON de primer nivel — hay que
        // envolverlo en un objeto antes de deserializar, y es más claro hacerlo
        // una vez acá que en cada lugar que liste algo.
        public static void ListarProyectos(
            Action<ProyectoResumenDto[]> alTenerExito,
            Action<string, string> alFallar)
        {
            var request = UnityWebRequest.Get(BaseUrl + "/proyectos");

            if (SesionManager.HaySesionActiva)
            {
                request.SetRequestHeader("Authorization", "Bearer " + SesionManager.Token);
            }

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonEnvuelto = "{\"proyectos\":" + request.downloadHandler.text + "}";
                    ProyectoResumenListaEnvoltorio envoltorio =
                        JsonUtility.FromJson<ProyectoResumenListaEnvoltorio>(jsonEnvuelto);
                    alTenerExito(envoltorio.proyectos);
                }
                else
                {
                    NotificarError(request.downloadHandler.text, alFallar);
                }

                request.Dispose();
            };
        }

        [Serializable]
        private class ProyectoResumenListaEnvoltorio
        {
            public ProyectoResumenDto[] proyectos;
        }

        private static void NotificarError(string cuerpoRespuesta, Action<string, string> alFallar)
        {
            // RNF-04: los errores internos no se muestran tal cual, solo mediante notificación.
            // Si el backend respondió con el formato de error esperado, ese mensaje ya es
            // apto para mostrar al usuario; si no (caída de red, backend abajo, etc.),
            // se usa un mensaje genérico.
            if (!string.IsNullOrEmpty(cuerpoRespuesta))
            {
                try
                {
                    ErrorResponse error = JsonUtility.FromJson<ErrorResponse>(cuerpoRespuesta);
                    if (error != null && !string.IsNullOrEmpty(error.codigo))
                    {
                        alFallar(error.error, error.codigo);
                        return;
                    }
                }
                catch (ArgumentException)
                {
                    // Respuesta no era el JSON de error esperado — cae al mensaje genérico.
                }
            }

            alFallar(MensajeErrorConexion, "ERROR_CONEXION");
        }
    }
}
