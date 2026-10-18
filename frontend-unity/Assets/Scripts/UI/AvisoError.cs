using TMPro;
using UnityEngine;

namespace Sme.UI
{
    public class AvisoError : MonoBehaviour
    {
        [SerializeField] private TMP_Text texto;

        public void Mostrar(string mensaje)
        {
            texto.text = mensaje;
            gameObject.SetActive(true);
        }

        public void Ocultar()
        {
            texto.text = string.Empty;
            gameObject.SetActive(false);
        }
    }
}
