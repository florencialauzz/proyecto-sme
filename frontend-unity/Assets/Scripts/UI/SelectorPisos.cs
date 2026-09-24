using Sme.Grid;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sme.UI
{
    // RF-18: un botón por piso del proyecto (PB, Piso 1, Piso 2...) para
    // elegir qué grilla se ve, y un botón para eliminar el piso que se está
    // mirando. Los botones de piso se arman por código, igual que
    // MenuContextual, porque su cantidad depende del proyecto —
    // contenedorBotones tiene que tener un HorizontalLayoutGroup (asignado en
    // el Inspector) que los acomode.
    //
    // Planta baja no se puede eliminar (ahí van la Entrada y la Salida), así
    // que el botón de eliminar se oculta mientras se mira planta baja.
    public class SelectorPisos : MonoBehaviour
    {
        private const float AnchoBoton = 110f;
        private const float AltoBoton = 40f;
        private static readonly Color ColorPisoConAdvertencias = new Color(1f, 0.4f, 0.4f, 1f);

        [SerializeField] private RectTransform contenedorBotones;
        [SerializeField] private Button botonEliminarPiso;

        // Awake y no Start: GrillaGenerador arma la grilla (y avisa por
        // AlCambiarPisos) en su Start, y todos los Awake de la escena corren
        // antes que cualquier Start — así la suscripción ya está hecha.
        private void Awake()
        {
            GrillaGenerador.AlCambiarPisos += Rearmar;
            ValidadorDiseno.AlValidar += AlValidar;
            botonEliminarPiso.onClick.AddListener(ConfirmarEliminarPiso);
        }

        private void OnDestroy()
        {
            GrillaGenerador.AlCambiarPisos -= Rearmar;
            ValidadorDiseno.AlValidar -= AlValidar;
        }

        // RF-20: el texto del botón de un piso con advertencias va en rojo,
        // así se ve que hay algo para corregir en un piso que no se está
        // mirando.
        private void AlValidar(ValidadorDiseno.Resultado resultado)
        {
            Rearmar();
        }

        private void Rearmar()
        {
            // Se desactivan antes de destruir: Destroy recién se ejecuta al
            // final del frame, y mientras tanto el HorizontalLayoutGroup los
            // seguiría contando junto a los nuevos.
            foreach (Transform hijo in contenedorBotones)
            {
                hijo.gameObject.SetActive(false);
                Destroy(hijo.gameObject);
            }

            for (int piso = 0; piso < GrillaGenerador.CantidadPisos; piso++)
            {
                CrearBotonPiso(piso);
            }

            // Con solo planta baja no hay nada que elegir: se oculta la fila
            // de botones. El botón de eliminar ya queda oculto porque en ese
            // caso el piso actual es siempre planta baja. Ocultar el
            // contenedor no corta la suscripción a AlCambiarPisos (se hizo en
            // Awake), así que si vuelve a haber más de un piso reaparece.
            contenedorBotones.gameObject.SetActive(GrillaGenerador.CantidadPisos > 1);
            botonEliminarPiso.gameObject.SetActive(GrillaGenerador.PisoActual != 0);
        }

        private void CrearBotonPiso(int piso)
        {
            GameObject boton = new GameObject($"BotonPiso_{piso}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            boton.transform.SetParent(contenedorBotones, false);

            LayoutElement layout = boton.GetComponent<LayoutElement>();
            layout.preferredWidth = AnchoBoton;
            layout.preferredHeight = AltoBoton;

            boton.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            GameObject textoGO = new GameObject("Texto", typeof(RectTransform));
            textoGO.transform.SetParent(boton.transform, false);
            RectTransform textoRect = textoGO.GetComponent<RectTransform>();
            textoRect.anchorMin = Vector2.zero;
            textoRect.anchorMax = Vector2.one;
            textoRect.offsetMin = Vector2.zero;
            textoRect.offsetMax = Vector2.zero;
            TMP_Text texto = textoGO.AddComponent<TextMeshProUGUI>();
            texto.text = piso == 0 ? "PB" : $"Piso {piso}";
            texto.alignment = TextAlignmentOptions.Center;
            ValidadorDiseno.Resultado validacion = ValidadorDiseno.UltimoResultado;
            bool tieneAdvertencias = validacion != null && validacion.PisosConAdvertencias.Contains(piso);
            texto.color = tieneAdvertencias ? ColorPisoConAdvertencias : Color.white;
            texto.fontSize = 18;
            texto.raycastTarget = false;

            // El piso que se está mirando queda deshabilitado: marca cuál es
            // y no tiene sentido volver a elegirlo.
            Button componenteBoton = boton.GetComponent<Button>();
            componenteBoton.interactable = piso != GrillaGenerador.PisoActual;
            componenteBoton.onClick.AddListener(() => GrillaGenerador.MostrarPiso(piso));
        }

        // Eliminar un piso borra todas sus piezas, así que se pide
        // confirmación con el mismo menú que usan las piezas con clic derecho.
        private void ConfirmarEliminarPiso()
        {
            int piso = GrillaGenerador.PisoActual;
            Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(null, botonEliminarPiso.transform.position);

            MenuContextual.Mostrar(botonEliminarPiso, posicionPantalla,
                new MenuContextual.Opcion($"Eliminar {GrillaGenerador.NombrePiso(piso)} y sus piezas", () => GrillaGenerador.EliminarPiso(piso)),
                new MenuContextual.Opcion("Cancelar", () => { }));
        }
    }
}
