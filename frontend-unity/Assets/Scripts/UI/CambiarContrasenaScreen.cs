using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class CambiarContrasenaScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoContrasenaActual;
        [SerializeField] private TMP_InputField campoContrasenaNueva;
        [SerializeField] private TMP_InputField campoConfirmacionContrasenaNueva;
        [SerializeField] private Button botonCambiarContrasena;
        [SerializeField] private AvisoError avisoError;

        [SerializeField] private UnityEvent alCambiarConExito;

        private void Awake()
        {
            botonCambiarContrasena.onClick.AddListener(CambiarContrasena);
        }

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
                    botonCambiarContrasena.interactable = true;
                    MostrarError(mensaje);
                });
        }

        private void MostrarError(string mensaje)
        {
            avisoError.Mostrar(mensaje);
        }

        private void OcultarError()
        {
            avisoError.Ocultar();
        }
    }
}
