using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-25: los dos modales con el resumen de indicadores de cada proyecto,
    // uno al lado del otro. Solo muestra: no marca cuál proyecto es mejor
    // (lo pide el caso de uso).
    public class PanelComparacion : MonoBehaviour
    {
        // El GameObject que se prende y apaga: el fondo con los dos modales.
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
