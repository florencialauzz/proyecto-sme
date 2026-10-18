using System;
using System.Collections;
using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    public class EditorScreen : MonoBehaviour
    {
        private const float DuracionMensajeSegundos = 2.5f;

        private const float DuracionErrorSegundos = 4f;

        [SerializeField] private Image fondoMensaje;
        [SerializeField] private TMP_Text iconoMensaje;
        [SerializeField] private TMP_Text textoMensaje;

        [SerializeField] private Button botonGuardar;
        [SerializeField] private Button botonSalir;
        [SerializeField] private Button botonConfiguracion;
        [SerializeField] private Button botonSimular;

        [SerializeField] private UnityEvent alSalir;
        [SerializeField] private UnityEvent alIrAConfiguracion;

        [SerializeField] private GameObject panelCambiosSinGuardar;
        [SerializeField] private Button botonGuardarYSalir;
        [SerializeField] private Button botonSalirSinGuardar;
        [SerializeField] private Button botonCancelarSalida;

        [SerializeField] private ModoReproduccion modoReproduccion;

        private Coroutine ocultamientoEnCurso;

        private bool simulacionEnCurso;

        private Action salidaPendiente;

        private void Awake()
        {
            MensajesEditor.Registrar(this);
            fondoMensaje.gameObject.SetActive(false);
            botonGuardar.onClick.AddListener(GuardarProyecto);
            botonSalir.onClick.AddListener(Salir);
            botonConfiguracion.onClick.AddListener(IrAConfiguracion);
            botonSimular.onClick.AddListener(Simular);
            botonGuardarYSalir.onClick.AddListener(GuardarYSalir);
            botonSalirSinGuardar.onClick.AddListener(SalirSinGuardar);
            botonCancelarSalida.onClick.AddListener(CerrarCambiosSinGuardar);
            panelCambiosSinGuardar.SetActive(false);

            ValidadorDiseno.AlValidar += AlValidar;
            ActualizarBotonSimular();
        }

        private void OnDestroy()
        {
            ValidadorDiseno.AlValidar -= AlValidar;
        }

        public void MostrarMensaje(string mensaje)
        {
            fondoMensaje.color = Tema.Exito;
            iconoMensaje.text = "✓";
            Mostrar(mensaje, DuracionMensajeSegundos);
        }

        public void MostrarError(string mensaje)
        {
            fondoMensaje.color = Tema.Peligro;
            iconoMensaje.text = "!";
            Mostrar(mensaje, DuracionErrorSegundos);
        }

        private void Mostrar(string mensaje, float duracionSegundos)
        {
            textoMensaje.text = mensaje;
            fondoMensaje.gameObject.SetActive(true);

            if (ocultamientoEnCurso != null)
            {
                StopCoroutine(ocultamientoEnCurso);
            }

            ocultamientoEnCurso = StartCoroutine(OcultarLuegoDe(duracionSegundos));
        }

        private IEnumerator OcultarLuegoDe(float segundos)
        {
            yield return new WaitForSeconds(segundos);
            fondoMensaje.gameObject.SetActive(false);
            ocultamientoEnCurso = null;
        }

        private void GuardarProyecto()
        {
            GuardarGrilla(
                alGuardar: () => MostrarMensaje("Proyecto guardado."),
                alFallar: null);
        }

        private void GuardarGrilla(Action alGuardar, Action alFallar)
        {
            botonGuardar.interactable = false;

            var request = new GuardarGrillaRequest
            {
                cantidadPisos = GrillaGenerador.CantidadPisos,
                piezas = GrillaGenerador.RecolectarPiezas()
            };

            ApiClient.Put<GuardarGrillaRequest, GuardarGrillaResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/grilla",
                request,
                alTenerExito: _ =>
                {
                    botonGuardar.interactable = true;
                    ProyectoManager.GuardarGrilla(request.piezas, request.cantidadPisos);
                    GrillaGenerador.MarcarDisenoGuardado();
                    alGuardar?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonGuardar.interactable = true;
                    MostrarError(mensaje);
                    alFallar?.Invoke();
                });
        }

        private void AlValidar(ValidadorDiseno.Resultado resultado)
        {
            ActualizarBotonSimular();
        }

        private void ActualizarBotonSimular()
        {
            ValidadorDiseno.Resultado ultimo = ValidadorDiseno.UltimoResultado;
            bool disenoValido = ultimo != null && ultimo.DisenoValido;
            botonSimular.interactable = disenoValido && !simulacionEnCurso;
        }

        private void Simular()
        {
            simulacionEnCurso = true;
            ActualizarBotonSimular();

            GuardarGrilla(
                alGuardar: EjecutarSimulacion,
                alFallar: TerminarSimulacionSinResultado);
        }

        private void EjecutarSimulacion()
        {
            ApiClient.Post<EjecutarSimulacionRequest, EjecutarSimulacionConReproduccionResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/simulacion/ejecutar",
                new EjecutarSimulacionRequest(),
                alTenerExito: respuesta =>
                {
                    EjecutarSimulacionResponse indicadores = SoloIndicadores(respuesta);
                    ProyectoManager.GuardarSimulacion(indicadores, yaGuardada: false, abiertoDesdeInicio: false);
                    modoReproduccion.Entrar(indicadores, respuesta.reproduccion);
                },
                alFallar: (mensaje, codigo) =>
                {
                    MostrarError(mensaje);
                    TerminarSimulacionSinResultado();
                });
        }

        private static EjecutarSimulacionResponse SoloIndicadores(EjecutarSimulacionConReproduccionResponse respuesta)
        {
            return new EjecutarSimulacionResponse
            {
                eficienciaEspacial = respuesta.eficienciaEspacial,
                calificacionEficiencia = respuesta.calificacionEficiencia,
                curvaOcupacion = respuesta.curvaOcupacion,
                demandaSatisfecha = respuesta.demandaSatisfecha,
                vehiculosRechazados = respuesta.vehiculosRechazados,
                periodosSaturacion = respuesta.periodosSaturacion,
                puntuacionGeneral = respuesta.puntuacionGeneral,
                calificacionTexto = respuesta.calificacionTexto
            };
        }

        private void TerminarSimulacionSinResultado()
        {
            simulacionEnCurso = false;
            ActualizarBotonSimular();
        }

        private void Salir()
        {
            SalirPreguntandoSiHayCambios(() => alSalir?.Invoke());
        }

        private void IrAConfiguracion()
        {
            SalirPreguntandoSiHayCambios(() =>
            {
                ProyectoManager.MarcarConfiguracionAbiertaDesdeEditor();
                alIrAConfiguracion?.Invoke();
            });
        }

        private void SalirPreguntandoSiHayCambios(Action salir)
        {
            if (!GrillaGenerador.HayCambiosSinGuardar())
            {
                salir();
                return;
            }

            salidaPendiente = salir;
            botonGuardarYSalir.interactable = true;
            panelCambiosSinGuardar.SetActive(true);
        }

        private void GuardarYSalir()
        {
            botonGuardarYSalir.interactable = false;
            GuardarGrilla(
                alGuardar: () => salidaPendiente(),
                alFallar: CerrarCambiosSinGuardar);
        }

        private void SalirSinGuardar()
        {
            salidaPendiente();
        }

        private void CerrarCambiosSinGuardar()
        {
            panelCambiosSinGuardar.SetActive(false);
        }
    }
}
