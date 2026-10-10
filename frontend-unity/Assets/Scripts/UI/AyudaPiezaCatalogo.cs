using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sme.UI
{
    // Va en cada tarjeta del catálogo del Editor: mientras el puntero está
    // encima, el recuadro de detalle (debajo del catálogo) explica qué es la
    // pieza y qué reglas tiene; al salir vuelve al texto por defecto. Los
    // textos de cada pieza se cargan en el Inspector. La tarjeta además se
    // tiñe con el color primario suave mientras el puntero está encima.
    public class AyudaPiezaCatalogo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const string TituloPorDefecto = "Catálogo de piezas";
        public const string DescripcionPorDefecto =
            "Pasá el puntero sobre una pieza para ver qué es, y arrastrala a la grilla para colocarla.";

        [SerializeField] private string nombrePieza;
        [TextArea]
        [SerializeField] private string descripcion;

        [SerializeField] private TMP_Text textoTituloDetalle;
        [SerializeField] private TMP_Text textoDescripcionDetalle;

        private Image fondo;

        private void Awake()
        {
            fondo = GetComponent<Image>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            fondo.color = Tema.PrimarioSuave;
            textoTituloDetalle.text = nombrePieza;
            textoDescripcionDetalle.text = descripcion;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            fondo.color = Tema.Superficie;
            textoTituloDetalle.text = TituloPorDefecto;
            textoDescripcionDetalle.text = DescripcionPorDefecto;
        }
    }
}
