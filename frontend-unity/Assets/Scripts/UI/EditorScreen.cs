using System.Collections;
using Sme.Managers;
using TMPro;
using UnityEngine;

namespace Sme.UI
{
    // Controlador de la escena Editor. Por ahora solo maneja el texto de
    // mensajes compartido (RF-13, ver MensajesEditor) — crece con RF-21 y
    // los RF de iteraciones siguientes.
    public class EditorScreen : MonoBehaviour
    {
        private const float DuracionMensajeSegundos = 2.5f;

        [SerializeField] private TMP_Text textoMensaje;

        private Coroutine ocultamientoEnCurso;

        private void Awake()
        {
            MensajesEditor.Registrar(this);
            textoMensaje.gameObject.SetActive(false);
        }

        public void MostrarMensaje(string mensaje)
        {
            textoMensaje.text = mensaje;
            textoMensaje.gameObject.SetActive(true);

            if (ocultamientoEnCurso != null)
            {
                StopCoroutine(ocultamientoEnCurso);
            }

            ocultamientoEnCurso = StartCoroutine(OcultarLuegoDe(DuracionMensajeSegundos));
        }

        private IEnumerator OcultarLuegoDe(float segundos)
        {
            yield return new WaitForSeconds(segundos);
            textoMensaje.gameObject.SetActive(false);
            ocultamientoEnCurso = null;
        }
    }
}
