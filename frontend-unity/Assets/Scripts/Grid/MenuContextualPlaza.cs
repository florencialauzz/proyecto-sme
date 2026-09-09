using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    // RF-14: menú contextual de una sola opción ("Marcar como accesible" /
    // "Quitar accesibilidad") que aparece al hacer clic derecho sobre una
    // Plaza colocada. Se construye por código en vez de prefab — es un botón
    // con texto, no justifica mantener un asset aparte solo para esto.
    //
    // El fondo invisible que cubre toda la pantalla es lo que permite cerrar
    // el menú clickeando afuera, sin necesidad de detectar "clic fuera de este
    // rectángulo" a mano.
    public static class MenuContextualPlaza
    {
        private const float Ancho = 220f;
        private const float Alto = 40f;

        public static void Mostrar(PiezaView pieza, Vector2 posicionPantalla)
        {
            Canvas canvasRaiz = pieza.GetComponentInParent<Canvas>().rootCanvas;

            GameObject fondo = new GameObject(
                "FondoMenuContextualPlaza", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Canvas), typeof(GraphicRaycaster));
            fondo.transform.SetParent(canvasRaiz.transform, false);
            RectTransform fondoRect = fondo.GetComponent<RectTransform>();
            fondoRect.anchorMin = Vector2.zero;
            fondoRect.anchorMax = Vector2.one;
            fondoRect.offsetMin = Vector2.zero;
            fondoRect.offsetMax = Vector2.zero;
            fondo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            fondo.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(fondo));

            // Cada Plaza dibuja su Canvas propio con sortingOrder 1 (PiezaView.Awake)
            // para quedar por encima de las celdas — este menú necesita un
            // sortingOrder más alto todavía para no quedar tapado por ninguna Plaza.
            Canvas canvasMenu = fondo.GetComponent<Canvas>();
            canvasMenu.overrideSorting = true;
            canvasMenu.sortingOrder = 2;

            GameObject opcion = new GameObject("OpcionMenuContextualPlaza", typeof(RectTransform), typeof(Image), typeof(Button));
            opcion.transform.SetParent(fondo.transform, false);
            RectTransform opcionRect = opcion.GetComponent<RectTransform>();
            opcionRect.sizeDelta = new Vector2(Ancho, Alto);
            opcionRect.pivot = new Vector2(0f, 1f);
            opcionRect.anchorMin = opcionRect.anchorMax = new Vector2(0.5f, 0.5f);
            opcion.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            GameObject textoGO = new GameObject("Texto", typeof(RectTransform));
            textoGO.transform.SetParent(opcion.transform, false);
            RectTransform textoRect = textoGO.GetComponent<RectTransform>();
            textoRect.anchorMin = Vector2.zero;
            textoRect.anchorMax = Vector2.one;
            textoRect.offsetMin = Vector2.zero;
            textoRect.offsetMax = Vector2.zero;
            TMP_Text texto = textoGO.AddComponent<TextMeshProUGUI>();
            texto.text = pieza.EsAccesible ? "Quitar accesibilidad" : "Marcar como accesible";
            texto.alignment = TextAlignmentOptions.Center;
            texto.color = Color.white;
            texto.fontSize = 18;
            texto.raycastTarget = false;

            opcion.GetComponent<Button>().onClick.AddListener(() =>
            {
                pieza.AlternarAccesibilidad();
                Object.Destroy(fondo);
            });

            // fondoRect cubre el mismo rectángulo que canvasRaiz, así que un
            // punto de pantalla convertido a coordenadas locales de canvasRaiz
            // sirve directo como anchoredPosition dentro de fondo.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRaiz.transform as RectTransform, posicionPantalla, canvasRaiz.worldCamera, out Vector2 posicionLocal);
            opcionRect.anchoredPosition = posicionLocal;
        }
    }
}
