using System;
using System.Text;
using Sme.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Sme.Managers
{
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

        public static void Put<TRequest, TResponse>(
            string ruta,
            TRequest cuerpo,
            Action<TResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            EnviarConCuerpo(UnityWebRequest.kHttpVerbPUT, ruta, cuerpo, alTenerExito, alFallar);
        }

        public static void Delete<TRequest, TResponse>(
            string ruta,
            TRequest cuerpo,
            Action<TResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            // UnityWebRequest.Delete no acepta body
            EnviarConCuerpo(UnityWebRequest.kHttpVerbDELETE, ruta, cuerpo, alTenerExito, alFallar);
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

        // JsonUtility no parsea un array de primer nivel, hay que envolverlo
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

        public static void ObtenerProyecto(
            long proyectoId,
            Action<ProyectoDetalleResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            var request = UnityWebRequest.Get(BaseUrl + "/proyectos/" + proyectoId);

            if (SesionManager.HaySesionActiva)
            {
                request.SetRequestHeader("Authorization", "Bearer " + SesionManager.Token);
            }

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProyectoDetalleResponse respuesta =
                        JsonUtility.FromJson<ProyectoDetalleResponse>(request.downloadHandler.text);
                    alTenerExito(respuesta);
                }
                else
                {
                    NotificarError(request.downloadHandler.text, alFallar);
                }

                request.Dispose();
            };
        }

        public static void ObtenerResultadoSimulacion(
            long proyectoId,
            Action<EjecutarSimulacionResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            var request = UnityWebRequest.Get(BaseUrl + "/proyectos/" + proyectoId + "/simulacion");

            if (SesionManager.HaySesionActiva)
            {
                request.SetRequestHeader("Authorization", "Bearer " + SesionManager.Token);
            }

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    EjecutarSimulacionResponse respuesta =
                        JsonUtility.FromJson<EjecutarSimulacionResponse>(request.downloadHandler.text);
                    alTenerExito(respuesta);
                }
                else
                {
                    NotificarError(request.downloadHandler.text, alFallar);
                }

                request.Dispose();
            };
        }

        public static void ObtenerPreguntaSeguridad(
            string nombreUsuario,
            Action<PreguntaSeguridadResponse> alTenerExito,
            Action<string, string> alFallar)
        {
            var request = UnityWebRequest.Get(BaseUrl + "/auth/pregunta-seguridad?nombreUsuario="
                + UnityWebRequest.EscapeURL(nombreUsuario));

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    PreguntaSeguridadResponse respuesta =
                        JsonUtility.FromJson<PreguntaSeguridadResponse>(request.downloadHandler.text);
                    alTenerExito(respuesta);
                }
                else
                {
                    NotificarError(request.downloadHandler.text, alFallar);
                }

                request.Dispose();
            };
        }

        private static void NotificarError(string cuerpoRespuesta, Action<string, string> alFallar)
        {
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
                }
            }

            alFallar(MensajeErrorConexion, "ERROR_CONEXION");
        }
    }
}
