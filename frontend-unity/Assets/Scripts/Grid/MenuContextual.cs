using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.Grid
{
    // Menú contextual genérico de clic derecho: una lista vertical de
    // botones con texto, cada uno con su propia acción. Antes había un
    // MenuContextualPlaza y un MenuContextualCalle casi idénticos — la
    // diferencia entre piezas está en QUÉ opciones arma cada una
    // (PiezaView/PiezaSimpleView deciden eso), no en cómo se arma el menú
    // en sí, así que ese armado de GameObjects se unificó acá.
    public static class MenuContextual
    {
        private const float Ancho = 220f;
        private const float Alto = 40f;

        public readonly struct Opcion
        {
            public readonly string Texto;
            public readonly System.Action Accion;

            public Opcion(string texto, System.Action accion)
            {
                Texto = texto;
                Accion = accion;
            }
        }

        public static void Mostrar(Component pieza, Vector2 posicionPantalla, params Opcion[] opciones)
        {
            Canvas canvasRaiz = pieza.GetComponentInParent<Canvas>().rootCanvas;

            GameObject fondo = new GameObject(
                "FondoMenuContextual", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Canvas), typeof(GraphicRaycaster));
            fondo.transform.SetParent(canvasRaiz.transform, false);
            RectTransform fondoRect = fondo.GetComponent<RectTransform>();
            fondoRect.anchorMin = Vector2.zero;
            fondoRect.anchorMax = Vector2.one;
            fondoRect.offsetMin = Vector2.zero;
            fondoRect.offsetMax = Vector2.zero;
            fondo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            fondo.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(fondo));

            // Cada pieza dibuja su Canvas propio con sortingOrder 1
            // (PiezaView.Awake / PiezaSimpleView.Awake) para quedar por
            // encima de las celdas — este menú necesita un sortingOrder más
            // alto todavía para no quedar tapado por ninguna pieza.
            Canvas canvasMenu = fondo.GetComponent<Canvas>();
            canvasMenu.overrideSorting = true;
            canvasMenu.sortingOrder = 2;

            // fondoRect cubre el mismo rectángulo que canvasRaiz, así que un
            // punto de pantalla convertido a coordenadas locales de
            // canvasRaiz sirve directo como anchoredPosition dentro de fondo.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRaiz.transform as RectTransform, posicionPantalla, canvasRaiz.worldCamera, out Vector2 posicionLocal);

            for (int i = 0; i < opciones.Length; i++)
            {
                Opcion opcion = opciones[i];

                GameObject boton = new GameObject($"OpcionMenuContextual_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                boton.transform.SetParent(fondo.transform, false);
                RectTransform botonRect = boton.GetComponent<RectTransform>();
                botonRect.sizeDelta = new Vector2(Ancho, Alto);
                botonRect.pivot = new Vector2(0f, 1f);
                botonRect.anchorMin = botonRect.anchorMax = new Vector2(0.5f, 0.5f);
                // Se apilan hacia abajo desde el punto de clic, una debajo
                // de la otra, en el orden en que se pasaron las opciones.
                botonRect.anchoredPosition = posicionLocal - new Vector2(0f, Alto * i);
                boton.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

                GameObject textoGO = new GameObject("Texto", typeof(RectTransform));
                textoGO.transform.SetParent(boton.transform, false);
                RectTransform textoRect = textoGO.GetComponent<RectTransform>();
                textoRect.anchorMin = Vector2.zero;
                textoRect.anchorMax = Vector2.one;
                textoRect.offsetMin = Vector2.zero;
                textoRect.offsetMax = Vector2.zero;
                TMP_Text texto = textoGO.AddComponent<TextMeshProUGUI>();
                texto.text = opcion.Texto;
                texto.alignment = TextAlignmentOptions.Center;
                texto.color = Color.white;
                texto.fontSize = 18;
                texto.raycastTarget = false;

                System.Action accion = opcion.Accion;
                boton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    accion();
                    Object.Destroy(fondo);
                });
            }
        }
    }
}
