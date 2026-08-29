using Sme.Managers;
using TMPro;
using UnityEngine;

namespace Sme.UI
{
    // Controlador de la escena Editor. Por ahora solo registra el texto de
    // mensajes compartido (RF-13, ver MensajesEditor) — crece con RF-21 y
    // los RF de iteraciones siguientes.
    public class EditorScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text textoMensaje;

        private void Awake()
        {
            MensajesEditor.Registrar(textoMensaje);
            textoMensaje.gameObject.SetActive(false);
        }
    }
}
