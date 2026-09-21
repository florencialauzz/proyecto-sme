using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-04: Cambiar contraseña. Requiere sesión iniciada (ApiClient adjunta el
    // token automáticamente). Requiere que los campos de abajo estén asignados
    // en el Inspector.
    public class CambiarContrasenaScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoContrasenaActual;
        [SerializeField] private TMP_InputField campoContrasenaNueva;
        [SerializeField] private TMP_InputField campoConfirmacionContrasenaNueva;
        [SerializeField] private Button botonCambiarContrasena;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alCambiarConExito;

        private void Awake()
        {
            botonCambiarContrasena.onClick.AddListener(CambiarContrasena);
        }

        // Este panel no recarga la escena al mostrarse de nuevo, así que Awake no
        // alcanza para limpiar un error que quedó de una visita anterior —
        // OnEnable corre cada vez que el panel se reactiva.
        private void OnEnable()
        {
            OcultarError();
        }

        private void CambiarContrasena()
        {
            OcultarError();
            botonCambiarContrasena.interactable = false;

            var request = new CambiarContrasenaRequest
            {
                contrasenaActual = campoContrasenaActual.text,
                contrasenaNueva = campoContrasenaNueva.text,
                confirmacionContrasenaNueva = campoConfirmacionContrasenaNueva.text
            };

            ApiClient.Put<CambiarContrasenaRequest, CambiarContrasenaResponse>(
                "/auth/contrasena",
                request,
                alTenerExito: _ =>
                {
                    botonCambiarContrasena.interactable = true;
                    alCambiarConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    // A1 (contraseña actual incorrecta) y A2 (confirmación no coincide)
                    // llegan acá según el "codigo" del error.
                    botonCambiarContrasena.interactable = true;
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
