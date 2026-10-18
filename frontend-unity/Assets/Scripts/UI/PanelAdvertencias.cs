using System.Text;
using Sme.Grid;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    public class PanelAdvertencias : MonoBehaviour
    {
        [SerializeField] private TMP_Text texto;
        [SerializeField] private Image fondoEstado;
        [SerializeField] private TMP_Text textoEstado;

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
