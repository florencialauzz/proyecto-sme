using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-05: Eliminar cuenta. Requiere sesión iniciada. Al confirmar con éxito
    // cierra la sesión local (la cuenta ya no existe en el backend) y avisa vía
    // el UnityEvent para volver a la pantalla de login (punto 6 del Flujo Principal).
    public class EliminarCuentaScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoContrasena;
        [SerializeField] private Button botonEliminarCuenta;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alEliminarConExito;

        private void Awake()
        {
            botonEliminarCuenta.onClick.AddListener(EliminarCuenta);
        }

        // Este panel no recarga la escena al mostrarse de nuevo, así que Awake no
        // alcanza para limpiar un error que quedó de una visita anterior —
        // OnEnable corre cada vez que el panel se reactiva.
        private void OnEnable()
        {
            OcultarError();
        }

        private void EliminarCuenta()
        {
            OcultarError();
            botonEliminarCuenta.interactable = false;

            var request = new EliminarCuentaRequest
            {
                contrasena = campoContrasena.text
            };

            ApiClient.Delete<EliminarCuentaRequest, EliminarCuentaResponse>(
                "/auth/cuenta",
                request,
                alTenerExito: _ =>
                {
                    botonEliminarCuenta.interactable = true;
                    SesionManager.CerrarSesion();
                    alEliminarConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    // A1: contraseña incorrecta, vuelve al punto 2 del Flujo Principal.
                    botonEliminarCuenta.interactable = true;
                    MostrarError(mensaje);
                });
        }

        private void MostrarError(string mensaje)
        {
            textoError.text = mensaje;
            textoError.gameObject.SetActive(true);
        }

        private void OcultarError()
        {
            textoError.text = string.Empty;
            textoError.gameObject.SetActive(false);
        }
    }
}
