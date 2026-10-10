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
    // Controlador de la escena Editor: mensajes compartidos (RF-13, ver
    // MensajesEditor), guardar proyecto (RF-21) y ejecutar la simulación
    // (RF-26), que deja la escena en modo reproducción (ModoReproduccion).
    public class EditorScreen : MonoBehaviour
    {
        private const float DuracionMensajeSegundos = 2.5f;

        // Los errores (colocación rechazada, guardado fallido) se muestran en
        // un aviso rojo y más tiempo que los avisos normales ("Proyecto
        // guardado.", en verde), para que no pasen desapercibidos.
        private const float DuracionErrorSegundos = 4f;

        // Aviso flotante abajo de la grilla: fondo de color, ícono y texto.
        // Lo que se prende y apaga es el fondo, que contiene a los otros dos.
        [SerializeField] private Image fondoMensaje;
        [SerializeField] private TMP_Text iconoMensaje;
        [SerializeField] private TMP_Text textoMensaje;

        [SerializeField] private Button botonGuardar;
        [SerializeField] private Button botonSalir;
        [SerializeField] private Button botonConfiguracion;
        [SerializeField] private Button botonSimular;

        [SerializeField] private UnityEvent alSalir;
        [SerializeField] private UnityEvent alIrAConfiguracion;

        // "¿Guardar los cambios?": se abre al ir a Inicio o a Configuración
        // si el diseño cambió desde el último guardado (las dos pantallas
        // vuelven a cargar el Editor desde lo guardado, así que lo que no se
        // guardó se pierde).
        [SerializeField] private GameObject panelCambiosSinGuardar;
        [SerializeField] private Button botonGuardarYSalir;
        [SerializeField] private Button botonSalirSinGuardar;
        [SerializeField] private Button botonCancelarSalida;

        // Animación: después de simular se entra a la reproducción, que es
        // la que lleva a Resultados.
        [SerializeField] private ModoReproduccion modoReproduccion;

        private Coroutine ocultamientoEnCurso;

        // Mientras se guarda y se simula, el botón de simular queda
        // deshabilitado aunque el diseño sea válido, para no mandar dos
        // simulaciones seguidas con un doble clic.
        private bool simulacionEnCurso;

        // A dónde se va después de responder "¿Guardar los cambios?".
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

            // RF-20: el botón de simular solo se habilita con el diseño
            // válido. La primera validación corre en el LateUpdate de
            // GrillaGenerador, después de este Awake, así que el botón
            // arranca deshabilitado y se actualiza con el evento.
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

        // RF-21: lo usan el botón Guardar y el de Simular — la simulación
        // corre sobre la grilla guardada en el backend, no sobre la que está
        // en pantalla, así que antes de simular hay que guardar.
        private void GuardarGrilla(Action alGuardar, Action alFallar)
        {
            botonGuardar.interactable = false;

            // cantidadPisos baja si se eliminó un piso desde el editor (RF-18)
            // — se guarda junto con las piezas, no antes.
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

        // --- RF-26: ejecutar simulación ---

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

        // Guarda la grilla y, si sale bien, pide la simulación. Los errores
        // del backend (configuración incompleta, diseño inválido) se muestran
        // igual que los del guardado.
        private void Simular()
        {
            simulacionEnCurso = true;
            ActualizarBotonSimular();

            GuardarGrilla(
                alGuardar: EjecutarSimulacion,
                alFallar: TerminarSimulacionSinResultado);
        }

        // simulacionEnCurso queda en true al salir bien: el Editor pasa a la
        // reproducción y el botón de simular ya no se vuelve a mostrar.
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

        // Los indicadores de la respuesta, sin reproduccion: es lo que
        // muestra Resultados y lo que se reenvía a /guardar, que no lleva la
        // reproducción (contratos/api-contract.md). Copia campo por campo
        // porque JsonUtility no deja omitir un campo al serializar.
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

        // El botón no guarda solo: si hay cambios sin guardar, pregunta
        // (panelCambiosSinGuardar) y el usuario decide.
        private void Salir()
        {
            SalirPreguntandoSiHayCambios(() => alSalir?.Invoke());
        }

        // RF-08 a RF-11: permite volver a la pantalla de Configuración para
        // cambiar frecuencia/permanencia/horario y simular otro escenario sobre
        // el mismo proyecto ya modelado.
        private void IrAConfiguracion()
        {
            SalirPreguntandoSiHayCambios(() =>
            {
                ProyectoManager.MarcarConfiguracionAbiertaDesdeEditor();
                alIrAConfiguracion?.Invoke();
            });
        }

        // --- Cambios sin guardar ---

        // En la reproducción la grilla es de solo lectura y se guardó antes
        // de simular: nunca hay cambios.
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

        // Si el guardado falla, el modal se cierra y el error se ve en el
        // aviso del Editor; el usuario sigue en el Editor con sus cambios.
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
