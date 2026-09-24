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
    // MensajesEditor) y guardar proyecto (RF-21).
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

        [SerializeField] private UnityEvent alSalir;
        [SerializeField] private UnityEvent alIrAConfiguracion;

        private Coroutine ocultamientoEnCurso;

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
                    MostrarMensaje("Proyecto guardado.");
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonGuardar.interactable = true;
                    MostrarError(mensaje);
                });
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
