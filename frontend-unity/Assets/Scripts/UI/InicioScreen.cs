using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-06: Cerrar sesión, RF-07: Ingresar nombre de proyecto. Requiere que los
    // campos de abajo estén asignados en el Inspector, sobre el Canvas de la
    // pantalla de inicio (a la que se llega después de iniciar sesión).
    public class InicioScreen : MonoBehaviour
    {
        [SerializeField] private Button botonCerrarSesion;

        [SerializeField] private TMP_InputField campoNombreProyecto;
        [SerializeField] private Button botonCrearProyecto;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private UnityEvent alCerrarSesion;
        [SerializeField] private UnityEvent alCrearProyectoConExito;

        private void Awake()
        {
            botonCerrarSesion.onClick.AddListener(CerrarSesion);
            botonCrearProyecto.onClick.AddListener(CrearProyecto);
            OcultarError();
        }

        private void CerrarSesion()
        {
            SesionManager.CerrarSesion();
            alCerrarSesion?.Invoke();
        }

        private void CrearProyecto()
        {
            OcultarError();
            botonCrearProyecto.interactable = false;

            var request = new CrearProyectoRequest
            {
                nombre = campoNombreProyecto.text
            };

            ApiClient.Post<CrearProyectoRequest, CrearProyectoResponse>(
                "/proyectos",
                request,
                alTenerExito: _ =>
                {
                    botonCrearProyecto.interactable = true;
                    alCrearProyectoConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    // A1 (nombre vacío) y A2 (nombre repetido) llegan acá según el
                    // "codigo" del error — el mensaje ya viene listo para mostrar.
                    botonCrearProyecto.interactable = true;
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
