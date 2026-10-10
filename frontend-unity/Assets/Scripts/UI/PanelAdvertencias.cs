using System.Text;
using Sme.Grid;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-20: lista en texto las advertencias del diseño (ValidadorDiseno), una
    // por renglón. Las celdas afectadas ya se ven en rojo sobre la grilla;
    // esto hace falta para lo que no es una celda (falta la Entrada, faltan
    // plazas accesibles) y para lo que está en un piso que no se está
    // mirando.
    //
    // Arriba de la lista hay una etiqueta de estado: verde "Listo para
    // simular" o roja con la cantidad de advertencias.
    public class PanelAdvertencias : MonoBehaviour
    {
        [SerializeField] private TMP_Text texto;
        [SerializeField] private Image fondoEstado;
        [SerializeField] private TMP_Text textoEstado;

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
                fondoEstado.color = Tema.ExitoSuave;
                textoEstado.color = Tema.Exito;
                textoEstado.text = "✓  Listo para simular";
                texto.color = Tema.TextoSecundario;
                texto.text = "El diseño cumple todas las validaciones. Ya podés ejecutar la simulación.";
                return;
            }

            int cantidad = resultado.Mensajes.Count;
            fondoEstado.color = Tema.PeligroSuave;
            textoEstado.color = Tema.PeligroOscuro;
            textoEstado.text = cantidad == 1 ? "1 advertencia" : $"{cantidad} advertencias";

            // Un punto rojo por advertencia; <indent> alinea los renglones
            // siguientes de una advertencia larga con el texto, no con el
            // punto. El aire entre advertencias es el paragraphSpacing del
            // texto (escena).
            string colorPunto = Tema.HexDe(Tema.Peligro);
            var lineas = new StringBuilder();
            foreach (string mensaje in resultado.Mensajes)
            {
                lineas.AppendLine($"<color={colorPunto}>●</color><indent=1.4em>{mensaje}</indent>");
            }
            texto.color = Tema.Texto;
            texto.text = lineas.ToString();
        }
    }
}
