using System.Collections.Generic;
using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class InicioScreen : MonoBehaviour
    {
        private const int MaximoSeleccionados = 2;

        [SerializeField] private Button botonCerrarSesion;

        [SerializeField] private TMP_Text textoNombreUsuario;
        [SerializeField] private TMP_Text textoInicialUsuario;

        [SerializeField] private TMP_InputField campoNombreProyecto;
        [SerializeField] private Button botonCrearProyecto;
        [SerializeField] private AvisoError avisoError;

        [SerializeField] private RectTransform contenedorProyectos;
        [SerializeField] private GameObject prefabItemProyecto;
        [SerializeField] private TMP_Text textoSinProyectos;
        [SerializeField] private Button botonAbrirProyecto;
        [SerializeField] private Button botonVerResultados;
        [SerializeField] private Button botonComparar;
        [SerializeField] private PanelComparacion panelComparacion;
        [SerializeField] private Button botonDuplicar;
        [SerializeField] private Button botonEliminar;

        [SerializeField] private GameObject panelConfirmarEliminar;
        [SerializeField] private TMP_Text textoConfirmarEliminar;
        [SerializeField] private Button botonConfirmarEliminar;
        [SerializeField] private Button botonCancelarEliminar;

        [SerializeField] private GameObject panelRenombrar;
        [SerializeField] private TMP_InputField campoNuevoNombre;
        [SerializeField] private Button botonConfirmarRenombrar;
        [SerializeField] private Button botonCancelarRenombrar;
        [SerializeField] private AvisoError avisoErrorRenombrar;

        [SerializeField] private UnityEvent alCerrarSesion;
        [SerializeField] private UnityEvent alCrearProyectoConExito;
        [SerializeField] private UnityEvent alAbrirProyectoConExito;
        [SerializeField] private UnityEvent alVerResultadosConExito;

        private readonly List<long> proyectosSeleccionados = new List<long>();
        private readonly Dictionary<long, Toggle> togglesPorProyecto = new Dictionary<long, Toggle>();
        private readonly Dictionary<long, string> nombresPorProyecto = new Dictionary<long, string>();

        private long proyectoARenombrar;

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
            botonConfirmarRenombrar.onClick.AddListener(Renombrar);
            botonCancelarRenombrar.onClick.AddListener(CerrarRenombrar);
            panelConfirmarEliminar.SetActive(false);
            panelRenombrar.SetActive(false);
            MostrarPerfil();
            ActualizarBotonesSeleccion();
            CargarProyectos();
        }

        private void MostrarPerfil()
        {
            string nombre = string.IsNullOrEmpty(SesionManager.NombreUsuario) ? "Usuario" : SesionManager.NombreUsuario;
            textoNombreUsuario.text = nombre;
            textoInicialUsuario.text = nombre.Substring(0, 1).ToUpperInvariant();
        }

        private void OnEnable()
        {
            OcultarError();
        }

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

                        Toggle toggle = item.GetComponent<Toggle>();
                        toggle.isOn = false;

                        long proyectoId = proyecto.proyectoId;
                        togglesPorProyecto[proyectoId] = toggle;
                        nombresPorProyecto[proyectoId] = proyecto.nombre;
                        toggle.onValueChanged.AddListener(seleccionado => SeleccionarProyecto(seleccionado, proyectoId));

                        Image fondoFila = item.GetComponent<Image>();
                        toggle.onValueChanged.AddListener(seleccionado => PintarFila(fondoFila, seleccionado));

                        DetectorClicDerecho clicDerecho = item.AddComponent<DetectorClicDerecho>();
                        clicDerecho.AlHacerClicDerecho += posicion => MostrarMenuProyecto(item.transform, posicion, proyectoId);
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

        private static void PintarFila(Image fondoFila, bool seleccionada)
        {
            if (fondoFila == null) return;
            fondoFila.color = seleccionada ? Tema.PrimarioSuave : Tema.Superficie;
        }

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

        private void MostrarMenuProyecto(Transform fila, Vector2 posicionPantalla, long proyectoId)
        {
            MenuContextual.Mostrar(fila, posicionPantalla,
                new MenuContextual.Opcion("Cambiar nombre", () => AbrirRenombrar(proyectoId)));
        }

        private void AbrirRenombrar(long proyectoId)
        {
            proyectoARenombrar = proyectoId;
            campoNuevoNombre.text = nombresPorProyecto[proyectoId];
            avisoErrorRenombrar.Ocultar();
            botonConfirmarRenombrar.interactable = true;
            panelRenombrar.SetActive(true);
            campoNuevoNombre.Select();
            campoNuevoNombre.ActivateInputField();
        }

        private void CerrarRenombrar()
        {
            panelRenombrar.SetActive(false);
        }

        private void Renombrar()
        {
            avisoErrorRenombrar.Ocultar();
            botonConfirmarRenombrar.interactable = false;

            ApiClient.Put<RenombrarProyectoRequest, RenombrarProyectoResponse>(
                $"/proyectos/{proyectoARenombrar}/nombre",
                new RenombrarProyectoRequest { nombre = campoNuevoNombre.text },
                alTenerExito: respuesta =>
                {
                    CerrarRenombrar();
                    CargarProyectos();
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonConfirmarRenombrar.interactable = true;
                    avisoErrorRenombrar.Mostrar(mensaje);
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
                    ProyectoManager.GuardarProyecto(
                        respuesta.proyectoId,
                        respuesta.filasGrilla,
                        respuesta.columnasGrilla);
                    alCrearProyectoConExito?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonCrearProyecto.interactable = true;
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
