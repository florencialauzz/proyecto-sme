using System.Collections.Generic;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-06: Cerrar sesión, RF-07: Ingresar nombre de proyecto, RF-22: Listar
    // proyectos, RF-24: ver los resultados guardados de un proyecto, RF-25:
    // Comparar resultados de proyectos, y duplicar o borrar un proyecto (sin
    // RF propio todavía, ver pendientes.md). Requiere que los campos de abajo
    // estén asignados en el Inspector, sobre el Canvas de la pantalla de
    // inicio (a la que se llega después de iniciar sesión).
    public class InicioScreen : MonoBehaviour
    {
        // RF-25 necesita dos proyectos seleccionados a la vez.
        private const int MaximoSeleccionados = 2;

        [SerializeField] private Button botonCerrarSesion;

        [SerializeField] private TMP_InputField campoNombreProyecto;
        [SerializeField] private Button botonCrearProyecto;
        [SerializeField] private TMP_Text textoError;

        [SerializeField] private RectTransform contenedorProyectos;
        [SerializeField] private GameObject prefabItemProyecto;
        [SerializeField] private TMP_Text textoSinProyectos;
        [SerializeField] private Button botonAbrirProyecto;
        [SerializeField] private Button botonVerResultados;
        [SerializeField] private Button botonComparar;
        [SerializeField] private PanelComparacion panelComparacion;
        [SerializeField] private Button botonDuplicar;
        [SerializeField] private Button botonEliminar;

        // Confirmación antes de borrar: no se puede deshacer.
        [SerializeField] private GameObject panelConfirmarEliminar;
        [SerializeField] private TMP_Text textoConfirmarEliminar;
        [SerializeField] private Button botonConfirmarEliminar;
        [SerializeField] private Button botonCancelarEliminar;

        [SerializeField] private UnityEvent alCerrarSesion;
        [SerializeField] private UnityEvent alCrearProyectoConExito;
        [SerializeField] private UnityEvent alAbrirProyectoConExito;
        [SerializeField] private UnityEvent alVerResultadosConExito;

        // En el orden en que se tildaron: si se tilda un tercero, se destilda
        // el más viejo.
        private readonly List<long> proyectosSeleccionados = new List<long>();
        private readonly Dictionary<long, Toggle> togglesPorProyecto = new Dictionary<long, Toggle>();
        private readonly Dictionary<long, string> nombresPorProyecto = new Dictionary<long, string>();

        private void Awake()
        {
            botonCerrarSesion.onClick.AddListener(CerrarSesion);
            botonCrearProyecto.onClick.AddListener(CrearProyecto);
            botonAbrirProyecto.onClick.AddListener(AbrirProyectoSeleccionado);
            botonVerResultados.onClick.AddListener(VerResultadosSeleccionado);
            botonComparar.onClick.AddListener(CompararSeleccionados);
            botonDuplicar.onClick.AddListener(DuplicarSeleccionado);
            botonEliminar.onClick.AddListener(PedirConfirmacionEliminar);
            botonConfirmarEliminar.onClick.AddListener(EliminarSeleccionado);
            botonCancelarEliminar.onClick.AddListener(CerrarConfirmacionEliminar);
            panelConfirmarEliminar.SetActive(false);
            ActualizarBotonesSeleccion();
            CargarProyectos();
        }

        // Este panel no recarga la escena al mostrarse de nuevo, así que Awake no
        // alcanza para limpiar un error que quedó de una visita anterior —
        // OnEnable corre cada vez que el panel se reactiva.
        private void OnEnable()
        {
            OcultarError();
        }

        // RF-22: la lista se trae al entrar a la pantalla, y de nuevo después
        // de duplicar o borrar un proyecto. Un proyecto recién creado navega
        // directo al Editor, no vuelve a esta pantalla.
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
                    proyectosSeleccionados.Clear();
                    togglesPorProyecto.Clear();
                    nombresPorProyecto.Clear();
                    ActualizarBotonesSeleccion();

                    foreach (ProyectoResumenDto proyecto in proyectos)
                    {
                        GameObject item = Instantiate(prefabItemProyecto, contenedorProyectos);
                        item.GetComponentInChildren<TMP_Text>().text = proyecto.nombre;

                        // Sin ToggleGroup: se pueden tildar hasta dos (RF-25).
                        Toggle toggle = item.GetComponent<Toggle>();
                        toggle.isOn = false;

                        long proyectoId = proyecto.proyectoId;
                        togglesPorProyecto[proyectoId] = toggle;
                        nombresPorProyecto[proyectoId] = proyecto.nombre;
                        toggle.onValueChanged.AddListener(seleccionado => SeleccionarProyecto(seleccionado, proyectoId));
                    }
                },
                alFallar: (mensaje, codigo) => MostrarError(mensaje));
        }

        private void SeleccionarProyecto(bool seleccionado, long proyectoId)
        {
            if (seleccionado)
            {
                proyectosSeleccionados.Add(proyectoId);
                if (proyectosSeleccionados.Count > MaximoSeleccionados)
                {
                    // Destildarlo dispara de nuevo este método con
                    // seleccionado = false, que lo saca de la lista.
                    long masViejo = proyectosSeleccionados[0];
                    togglesPorProyecto[masViejo].isOn = false;
                }
            }
            else
            {
                proyectosSeleccionados.Remove(proyectoId);
            }

            ActualizarBotonesSeleccion();
        }

        // Abrir, Ver resultados, Duplicar y Eliminar actúan sobre un solo
        // proyecto; Comparar, sobre dos.
        private void ActualizarBotonesSeleccion()
        {
            bool unoSeleccionado = proyectosSeleccionados.Count == 1;
            bool dosSeleccionados = proyectosSeleccionados.Count == 2;

            botonAbrirProyecto.interactable = unoSeleccionado;
            botonVerResultados.interactable = unoSeleccionado;
            botonDuplicar.interactable = unoSeleccionado;
            botonEliminar.interactable = unoSeleccionado;
            botonComparar.interactable = dosSeleccionados;
        }

        private void AbrirProyectoSeleccionado()
        {
            if (proyectosSeleccionados.Count != 1) return;
            AbrirProyecto(proyectosSeleccionados[0]);
        }

        // Abrir un proyecto guardado para seguir editándolo: trae la grilla
        // completa (GET /proyectos/{id}) y la deja lista para que GrillaGenerador
        // la reconstruya al entrar a la escena Editor.
        private void AbrirProyecto(long proyectoId)
        {
            OcultarError();
            botonAbrirProyecto.interactable = false;

            ApiClient.ObtenerProyecto(
                proyectoId,
                alTenerExito: detalle =>
                {
                    CargarProyectoEnMemoria(detalle);
                    alAbrirProyectoConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    ActualizarBotonesSeleccion();
                    MostrarError(mensaje);
                });
        }

        // RF-24: "el usuario accede a la pantalla de resultados del proyecto".
        // Trae el proyecto (la pantalla necesita el horario y las plazas para
        // la curva) y después sus resultados guardados.
        private void VerResultadosSeleccionado()
        {
            if (proyectosSeleccionados.Count != 1) return;
            long proyectoId = proyectosSeleccionados[0];

            OcultarError();
            botonVerResultados.interactable = false;

            ApiClient.ObtenerProyecto(
                proyectoId,
                alTenerExito: detalle =>
                {
                    ApiClient.ObtenerResultadoSimulacion(
                        proyectoId,
                        alTenerExito: resultado =>
                        {
                            CargarProyectoEnMemoria(detalle);
                            ProyectoManager.GuardarSimulacion(resultado, yaGuardada: true, abiertoDesdeInicio: true);
                            alVerResultadosConExito?.Invoke();
                        },
                        alFallar: (mensaje, codigo) =>
                        {
                            ActualizarBotonesSeleccion();
                            MostrarError(mensaje);
                        });
                },
                alFallar: (mensaje, codigo) =>
                {
                    ActualizarBotonesSeleccion();
                    MostrarError(mensaje);
                });
        }

        // RF-25: trae los resultados guardados de los dos proyectos, uno
        // después del otro, y los muestra en los modales. Si alguno no tiene
        // resultados, Flujo Alternativo A1: aviso y se vuelve a la lista.
        private void CompararSeleccionados()
        {
            if (proyectosSeleccionados.Count != 2) return;
            long primerId = proyectosSeleccionados[0];
            long segundoId = proyectosSeleccionados[1];

            OcultarError();
            botonComparar.interactable = false;

            ApiClient.ObtenerResultadoSimulacion(
                primerId,
                alTenerExito: primerResultado =>
                {
                    ApiClient.ObtenerResultadoSimulacion(
                        segundoId,
                        alTenerExito: segundoResultado =>
                        {
                            ActualizarBotonesSeleccion();
                            panelComparacion.Mostrar(
                                nombresPorProyecto[primerId], primerResultado,
                                nombresPorProyecto[segundoId], segundoResultado);
                        },
                        alFallar: NotificarErrorComparacion);
                },
                alFallar: NotificarErrorComparacion);
        }

        private void NotificarErrorComparacion(string mensaje, string codigo)
        {
            ActualizarBotonesSeleccion();

            if (codigo == "SIN_RESULTADOS")
            {
                MostrarError("Ambos proyectos deben contar con resultados de simulación guardados para poder compararlos.");
            }
            else
            {
                MostrarError(mensaje);
            }
        }

        // Duplicar: el backend copia la configuración y la grilla (no los
        // resultados) con el nombre "<nombre> (copia)". La copia aparece al
        // recargar la lista.
        private void DuplicarSeleccionado()
        {
            if (proyectosSeleccionados.Count != 1) return;
            long proyectoId = proyectosSeleccionados[0];

            OcultarError();
            botonDuplicar.interactable = false;

            ApiClient.Post<DuplicarProyectoRequest, DuplicarProyectoResponse>(
                $"/proyectos/{proyectoId}/duplicar",
                new DuplicarProyectoRequest(),
                alTenerExito: respuesta => CargarProyectos(),
                alFallar: (mensaje, codigo) =>
                {
                    ActualizarBotonesSeleccion();
                    MostrarError(mensaje);
                });
        }

        private void PedirConfirmacionEliminar()
        {
            if (proyectosSeleccionados.Count != 1) return;
            long proyectoId = proyectosSeleccionados[0];

            textoConfirmarEliminar.text =
                $"¿Eliminar el proyecto \"{nombresPorProyecto[proyectoId]}\"? Se borran su diseño y sus resultados, y no se puede deshacer.";
            botonConfirmarEliminar.interactable = true;
            panelConfirmarEliminar.SetActive(true);
        }

        private void CerrarConfirmacionEliminar()
        {
            panelConfirmarEliminar.SetActive(false);
        }

        private void EliminarSeleccionado()
        {
            if (proyectosSeleccionados.Count != 1) return;
            long proyectoId = proyectosSeleccionados[0];

            OcultarError();
            botonConfirmarEliminar.interactable = false;

            ApiClient.Delete<EliminarProyectoRequest, EliminarProyectoResponse>(
                $"/proyectos/{proyectoId}",
                new EliminarProyectoRequest(),
                alTenerExito: respuesta =>
                {
                    CerrarConfirmacionEliminar();
                    CargarProyectos();
                },
                alFallar: (mensaje, codigo) =>
                {
                    CerrarConfirmacionEliminar();
                    MostrarError(mensaje);
                });
        }

        private static void CargarProyectoEnMemoria(ProyectoDetalleResponse detalle)
        {
            ProyectoManager.GuardarProyecto(
                detalle.proyectoId,
                detalle.filasGrilla,
                detalle.columnasGrilla,
                detalle.estado,
                detalle.piezas,
                detalle.cantidadPisos,
                detalle.frecuenciaIngreso,
                detalle.tiempoPermanencia,
                detalle.horaInicioSimulacion,
                detalle.horaFinSimulacion,
                detalle.conFluctuaciones);
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
