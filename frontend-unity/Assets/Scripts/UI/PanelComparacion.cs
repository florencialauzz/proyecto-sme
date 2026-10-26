using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    public class PanelComparacion : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        [SerializeField] private TMP_Text tituloIzquierdo;
        [SerializeField] private TMP_Text cuerpoIzquierdo;
        [SerializeField] private TMP_Text tituloDerecho;
        [SerializeField] private TMP_Text cuerpoDerecho;

        [SerializeField] private Button botonCerrar;

        private void Awake()
        {
            botonCerrar.onClick.AddListener(Cerrar);
            panel.SetActive(false);
        }

        public void Mostrar(string nombreIzquierdo, EjecutarSimulacionResponse resultadoIzquierdo,
            string nombreDerecho, EjecutarSimulacionResponse resultadoDerecho)
        {
            tituloIzquierdo.text = nombreIzquierdo;
            cuerpoIzquierdo.text = FormatoIndicadores.Resumen(resultadoIzquierdo);
            tituloDerecho.text = nombreDerecho;
            cuerpoDerecho.text = FormatoIndicadores.Resumen(resultadoDerecho);
            panel.SetActive(true);
        }

        private void Cerrar()
        {
            panel.SetActive(false);
        }
    }
}
