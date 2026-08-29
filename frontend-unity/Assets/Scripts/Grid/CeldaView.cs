using UnityEngine;

namespace Sme.Grid
{
    // RF-12: una celda de la grilla. Guarda su posición lógica (fila, columna)
    // para que RF-13 (colocar/quitar plaza) sepa sobre qué celda se soltó una pieza.
    public class CeldaView : MonoBehaviour
    {
        public int Fila { get; private set; }
        public int Columna { get; private set; }

        // La pieza colocada (si hay) es hija de este transform — no hace falta
        // guardar la referencia por separado, alcanza con preguntarle a Unity.
        public bool Ocupada { get; private set; }

        public void Inicializar(int fila, int columna)
        {
            Fila = fila;
            Columna = columna;
        }

        public void Ocupar()
        {
            Ocupada = true;
        }

        public void Liberar()
        {
            Ocupada = false;
        }
    }
}
