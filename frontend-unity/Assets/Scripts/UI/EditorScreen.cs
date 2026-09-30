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
    // (RF-26).
    public class EditorScreen : MonoBehaviour
    {
        private const float DuracionMensajeSegundos = 2.5f;

        // Los errores (colocación rechazada, guardado fallido) se muestran en
        // rojo, en negrita, un poco más grandes y más tiempo que los avisos
        // normales ("Proyecto guardado."), para que no pasen desapercibidos.
        private const float DuracionErrorSegundos = 4f;
        private const float AumentoTamanioError = 4f;
        private static readonly Color ColorError = new Color(0.95f, 0.25f, 0.25f, 1f);

        [SerializeField] private TMP_Text textoMensaje;
        [SerializeField] private Button botonGuardar;
        [SerializeField] private Button botonSalir;
        [SerializeField] private Button botonConfiguracion;
        [SerializeField] private Button botonSimular;

        [SerializeField] private UnityEvent alSalir;
        [SerializeField] private UnityEvent alIrAConfiguracion;
        [SerializeField] private UnityEvent alIrAResultados;

        private Coroutine ocultamientoEnCurso;

        // Mientras se guarda y se simula, el botón de simular queda
        // deshabilitado aunque el diseño sea válido, para no mandar dos
        // simulaciones seguidas con un doble clic.
        private bool simulacionEnCurso;

        // Estilo del texto tal como está en la escena: es el de los avisos
        // normales, y a él se vuelve después de mostrar un error.
        private Color colorNormal;
        private float tamanioNormal;

        private void Awake()
        {
            MensajesEditor.Registrar(this);
            colorNormal = textoMensaje.color;
            tamanioNormal = textoMensaje.fontSize;
            textoMensaje.gameObject.SetActive(false);
            botonGuardar.onClick.AddListener(GuardarProyecto);
            botonSalir.onClick.AddListener(Salir);
            botonConfiguracion.onClick.AddListener(IrAConfiguracion);
            botonSimular.onClick.AddListener(Simular);

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
            textoMensaje.color = colorNormal;
            textoMensaje.fontSize = tamanioNormal;
            textoMensaje.fontStyle = FontStyles.Normal;
            Mostrar(mensaje, DuracionMensajeSegundos);
        }

        public void MostrarError(string mensaje)
        {
            textoMensaje.color = ColorError;
            textoMensaje.fontSize = tamanioNormal + AumentoTamanioError;
            textoMensaje.fontStyle = FontStyles.Bold;
            Mostrar(mensaje, DuracionErrorSegundos);
        }

        private void Mostrar(string mensaje, float duracionSegundos)
        {
            textoMensaje.text = mensaje;
            textoMensaje.gameObject.SetActive(true);

            if (ocultamientoEnCurso != null)
            {
                StopCoroutine(ocultamientoEnCurso);
            }

            ocultamientoEnCurso = StartCoroutine(OcultarLuegoDe(duracionSegundos));
        }

        private IEnumerator OcultarLuegoDe(float segundos)
        {
            yield return new WaitForSeconds(segundos);
            textoMensaje.gameObject.SetActive(false);
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

        private void EjecutarSimulacion()
        {
            ApiClient.Post<EjecutarSimulacionRequest, EjecutarSimulacionResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/simulacion/ejecutar",
                new EjecutarSimulacionRequest(),
                alTenerExito: resultado =>
                {
                    ProyectoManager.GuardarSimulacion(resultado);
                    alIrAResultados?.Invoke();
                },
                alFallar: (mensaje, codigo) =>
                {
                    MostrarError(mensaje);
                    TerminarSimulacionSinResultado();
                });
        }

        private void TerminarSimulacionSinResultado()
        {
            simulacionEnCurso = false;
            ActualizarBotonSimular();
        }

        // El botón no guarda solo: si hay cambios sin guardar, es el usuario
        // quien decide si vuelve a Inicio de todos modos.
        private void Salir()
        {
            alSalir?.Invoke();
        }

        // RF-08 a RF-11: permite volver a la pantalla de Configuración para
        // cambiar frecuencia/permanencia/horario y simular otro escenario sobre
        // el mismo proyecto ya modelado.
        private void IrAConfiguracion()
        {
            alIrAConfiguracion?.Invoke();
        }
    }
}
