using Sme.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    public class CeldaView : MonoBehaviour
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

        public int Piso { get; private set; }
        public int Fila { get; private set; }
        public int Columna { get; private set; }
        public CeldaGrilla Modelo { get; private set; }
        public bool Ocupada => Modelo.Ocupada;

        public void Inicializar(int piso, int fila, int columna)
        {
            Piso = piso;
            Fila = fila;
            Columna = columna;
            Modelo = GrillaModelo.ObtenerCelda(piso, fila, columna);
        }

        private static readonly Color ColorAdvertencia = new Color(Tema.Peligro.r, Tema.Peligro.g, Tema.Peligro.b, 0.42f);
        private GameObject marcaAdvertencia;

        public void Ocupar(TipoPieza tipo, CaraAcceso? direccion)
        {
            Modelo.Ocupar(tipo, direccion);
            NotificarVecinasYPropia();
            ValidadorDiseno.PedirValidacion();
        }

        public void OcuparComoRampa(CaraAcceso direccion, SentidoVertical sentidoVertical, bool esEntrada)
        {
            Modelo.OcuparComoRampa(direccion, sentidoVertical, esEntrada);
            NotificarVecinasYPropia();
            ValidadorDiseno.PedirValidacion();
        }

        public void Liberar()
        {
            Modelo.Liberar();
            NotificarVecinasYPropia();
            ValidadorDiseno.PedirValidacion();
        }

        public void MarcarAccesible(bool esAccesible)
        {
            Modelo.MarcarAccesible(esAccesible);
            ValidadorDiseno.PedirValidacion();
        }

        public void MostrarAdvertencia(bool mostrar)
        {
            if (!mostrar)
            {
                if (marcaAdvertencia != null)
                {
                    marcaAdvertencia.SetActive(false);
                }
                return;
            }

            if (marcaAdvertencia == null)
            {
                marcaAdvertencia = new GameObject("MarcaAdvertencia", typeof(RectTransform), typeof(Image));
                marcaAdvertencia.transform.SetParent(transform, false);
                RectTransform rect = marcaAdvertencia.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image imagen = marcaAdvertencia.GetComponent<Image>();
                imagen.color = ColorAdvertencia;
                imagen.raycastTarget = false;

                Canvas canvas = marcaAdvertencia.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 2;
            }
            marcaAdvertencia.SetActive(true);
        }

        private void NotificarVecinasYPropia()
        {
            Refrescar();
            foreach (CaraAcceso direccion in Direcciones)
            {
                (int deltaFila, int deltaColumna) = GrillaModelo.Delta(direccion);
                GrillaGenerador.ObtenerCelda(Piso, Fila + deltaFila, Columna + deltaColumna)?.Refrescar();
            }
        }

        private void Refrescar()
        {
            GetComponentInChildren<IPiezaColocada>(true)?.Refrescar();
        }
    }
}
