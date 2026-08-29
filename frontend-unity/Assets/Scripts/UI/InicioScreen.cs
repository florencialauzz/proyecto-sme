using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-06: Cerrar sesión, RF-07: Ingresar nombre de proyecto, RF-22: Listar
    // proyectos. Requiere que los campos de abajo estén asignados en el
    // Inspector, sobre el Canvas de la pantalla de inicio (a la que se llega
    // después de iniciar sesión).
    public class InicioScreen : MonoBehaviour
    {
        [SerializeField] private Button botonCerrarSesion;

        [SerializeField] private TMP_InputField campoNombreProyecto;
        [SerializeField] private Button botonCrearProyecto;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private RectTransform contenedorProyectos;
        [SerializeField] private GameObject prefabItemProyecto;
        [SerializeField] private TMP_Text textoSinProyectos;

        [SerializeField] private UnityEvent alCerrarSesion;
        [SerializeField] private UnityEvent alCrearProyectoConExito;
        [SerializeField] private UnityEvent alAbrirProyectoConExito;

        private void Awake()
        {
            botonCerrarSesion.onClick.AddListener(CerrarSesion);
            botonCrearProyecto.onClick.AddListener(CrearProyecto);
            OcultarError();
            CargarProyectos();
        }

        // RF-22: la lista se trae una vez al entrar a la pantalla — no hace
        // falta re-consultarla dentro de la sesión, un proyecto recién creado
        // navega directo al Editor, no vuelve a esta pantalla.
        private void CargarProyectos()
        {
            ApiClient.ListarProyectos(
                alTenerExito: proyectos =>
                {
                    foreach (Transform hijo in contenedorProyectos)
                    {
                        Destroy(hijo.gameObject);
                    }

                    textoSinProyectos.gameObject.SetActive(proyectos.Length == 0);

                    foreach (ProyectoResumenDto proyecto in proyectos)
                    {
                        GameObject item = Instantiate(prefabItemProyecto, contenedorProyectos);
                        item.GetComponentInChildren<TMP_Text>().text = proyecto.nombre;

                        long proyectoId = proyecto.proyectoId;
                        item.GetComponent<Button>().onClick.AddListener(() => AbrirProyecto(proyectoId));
                    }
                },
                alFallar: (mensaje, codigo) => MostrarError(mensaje));
        }

        // Abrir un proyecto guardado para seguir editándolo: trae la grilla
        // completa (GET /proyectos/{id}) y la deja lista para que GrillaGenerador
        // la reconstruya al entrar a la escena Editor.
        private void AbrirProyecto(long proyectoId)
        {
            ApiClient.ObtenerProyecto(
                proyectoId,
                alTenerExito: detalle =>
                {
                    ProyectoManager.GuardarProyecto(
                        detalle.proyectoId,
                        detalle.filasGrilla,
                        detalle.columnasGrilla,
                        detalle.estado,
                        detalle.piezas);
                    alAbrirProyectoConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) => MostrarError(mensaje));
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
                alTenerExito: respuesta =>
                {
                    botonCrearProyecto.interactable = true;
                    // RF-12: la escena Editor lee estos datos desde ProyectoManager
                    // para generar la grilla (GrillaGenerador.Start).
                    ProyectoManager.GuardarProyecto(
                        respuesta.proyectoId,
                        respuesta.filasGrilla,
                        respuesta.columnasGrilla,
                        respuesta.estado);
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
