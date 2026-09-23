using UnityEngine;

namespace Sme.Grid
{
    // RF-12: una celda de la grilla. El estado lógico (qué pieza hay, hacia
    // dónde apunta) vive en Modelo (GrillaModelo) — esta vista solo sabe
    // dónde está y avisa a sus vecinas cuando algo cambió.
    public class CeldaView : MonoBehaviour
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

        public int Fila { get; private set; }
        public int Columna { get; private set; }
        public CeldaGrilla Modelo { get; private set; }
        public bool Ocupada => Modelo.Ocupada;

        public void Inicializar(int fila, int columna)
        {
            Fila = fila;
            Columna = columna;
            Modelo = GrillaModelo.ObtenerCelda(fila, columna);
        }

        public void Ocupar(TipoPieza tipo, CaraAcceso? direccion)
        {
            Modelo.Ocupar(tipo, direccion);
            NotificarVecinasYPropia();
        }

        public void Liberar()
        {
            Modelo.Liberar();
            NotificarVecinasYPropia();
        }

        // Autotiling y rol de cruce (editor/grafo-circulacion.md) dependen de
        // hasta 4 vecinas — colocar, mover o quitar tiene que hacer que se
        // vuelvan a dibujar, no solo la celda afectada.
        private void NotificarVecinasYPropia()
        {
            Refrescar();
            foreach (CaraAcceso direccion in Direcciones)
            {
                (int deltaFila, int deltaColumna) = GrillaModelo.Delta(direccion);
                GrillaGenerador.ObtenerCelda(Fila + deltaFila, Columna + deltaColumna)?.Refrescar();
            }
        }

        private void Refrescar()
        {
            GetComponentInChildren<IPiezaColocada>()?.Refrescar();
        }
    }
}
