using TMPro;
using UnityEngine;

namespace Sme.UI
{
    // Recuadro de error de los formularios (login, registro, configuración,
    // etc.): fondo rojo suave, ícono y el mensaje que devuelve el backend.
    // Va en el GameObject del recuadro, que es el que se prende y se apaga —
    // el texto es un hijo, porque un mismo GameObject no puede tener a la vez
    // la Image del fondo y el texto.
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
