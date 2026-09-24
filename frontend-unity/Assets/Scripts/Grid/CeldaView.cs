using UnityEngine;
using UnityEngine.UI;

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

        // Marca roja de RF-20 (ver MostrarAdvertencia). Se crea recién la
        // primera vez que hace falta.
        private static readonly Color ColorAdvertencia = new Color(0.9f, 0.1f, 0.1f, 0.4f);
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

        // RF-14: la accesibilidad de una Plaza cambia la cuenta de RF-20
        // (mínimo de plazas accesibles), aunque no cambie ninguna celda.
        public void MarcarAccesible(bool esAccesible)
        {
            Modelo.MarcarAccesible(esAccesible);
            ValidadorDiseno.PedirValidacion();
        }

        // RF-20: resalta la celda en rojo, por encima de la pieza que tenga.
        // Canvas propio con sortingOrder 2 por el mismo motivo que las piezas
        // usan 1 (ver PiezaView.Awake): dibujarse encima sin tocar el orden
        // de hermanos que necesita el GridLayoutGroup. No recibe raycasts,
        // así la pieza de abajo se sigue pudiendo arrastrar.
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

        // Autotiling y rol de cruce (editor/grafo-circulacion.md) dependen de
        // hasta 4 vecinas del mismo piso — colocar, mover o quitar tiene que
        // hacer que se vuelvan a dibujar, no solo la celda afectada.
        private void NotificarVecinasYPropia()
        {
            Refrescar();
            foreach (CaraAcceso direccion in Direcciones)
            {
                (int deltaFila, int deltaColumna) = GrillaModelo.Delta(direccion);
                GrillaGenerador.ObtenerCelda(Piso, Fila + deltaFila, Columna + deltaColumna)?.Refrescar();
            }
        }

        // includeInactive: una Rampa o Escalera colocada mirando un piso
        // también ocupa celdas en pisos ocultos (RF-18), y las calles vecinas
        // de esos pisos tienen que recalcular su autotiling igual.
        private void Refrescar()
        {
            GetComponentInChildren<IPiezaColocada>(true)?.Refrescar();
        }
    }
}
