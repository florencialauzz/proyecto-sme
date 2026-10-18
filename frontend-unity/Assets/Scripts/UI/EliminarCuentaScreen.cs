using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class EliminarCuentaScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField campoContrasena;
        [SerializeField] private Button botonEliminarCuenta;
        [SerializeField] private AvisoError avisoError;

        [SerializeField] private UnityEvent alEliminarConExito;

        private void Awake()
        {
            botonEliminarCuenta.onClick.AddListener(EliminarCuenta);
        }

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
                    botonEliminarCuenta.interactable = true;
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
