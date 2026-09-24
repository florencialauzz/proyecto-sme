using System.Text;
using Sme.Grid;
using TMPro;
using UnityEngine;

namespace Sme.UI
{
    // RF-20: lista en texto las advertencias del diseño (ValidadorDiseno), una
    // por renglón. Las celdas afectadas ya se ven en rojo sobre la grilla;
    // esto hace falta para lo que no es una celda (falta la Entrada, faltan
    // plazas accesibles) y para lo que está en un piso que no se está
    // mirando.
    public class PanelAdvertencias : MonoBehaviour
    {
        private static readonly Color ColorAdvertencia = new Color(0.95f, 0.3f, 0.3f, 1f);
        private static readonly Color ColorSinAdvertencias = new Color(0.45f, 0.85f, 0.5f, 1f);

        [SerializeField] private TMP_Text texto;

        // Awake y no Start: GrillaGenerador corre la primera validación en su
        // LateUpdate, pero así la suscripción está hecha antes de cualquier
        // cosa de la escena.
        private void Awake()
        {
            ValidadorDiseno.AlValidar += Mostrar;
        }

        private void OnDestroy()
        {
            ValidadorDiseno.AlValidar -= Mostrar;
        }

        private void Mostrar(ValidadorDiseno.Resultado resultado)
        {
            if (resultado.DisenoValido)
            {
                texto.color = ColorSinAdvertencias;
                texto.text = "Sin advertencias: el diseño se puede simular.";
                return;
            }

            var lineas = new StringBuilder();
            foreach (string mensaje in resultado.Mensajes)
            {
                lineas.AppendLine("• " + mensaje);
            }
            texto.color = ColorAdvertencia;
            texto.text = lineas.ToString();
        }
    }
}
