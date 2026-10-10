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
    // que el botón de eliminar se oculta mientras se mira planta baja. También
    // se oculta con la grilla en solo lectura (GrillaGenerador.SoloLectura).
    public class SelectorPisos : MonoBehaviour
    {
        private const float AnchoBoton = 104f;
        private const float AltoBoton = 36f;

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

            // Mientras se reproduce la simulación (Animación) se puede
            // cambiar de piso pero no eliminarlo.
            bool puedeEliminar = !GrillaGenerador.SoloLectura && GrillaGenerador.PisoActual != 0;
            botonEliminarPiso.gameObject.SetActive(puedeEliminar);
        }

        private void CrearBotonPiso(int piso)
        {
            GameObject boton = new GameObject($"BotonPiso_{piso}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            boton.transform.SetParent(contenedorBotones, false);

            LayoutElement layout = boton.GetComponent<LayoutElement>();
            layout.preferredWidth = AnchoBoton;
            layout.preferredHeight = AltoBoton;

            GameObject textoGO = new GameObject("Texto", typeof(RectTransform));
            textoGO.transform.SetParent(boton.transform, false);
            textoGO.AddComponent<TextMeshProUGUI>();
            Tema.Estirar(textoGO.GetComponent<RectTransform>());

            // Pestañas: la del piso que se está mirando va rellena con el
            // color primario; las otras, sin fondo. Queda deshabilitada
            // porque no tiene sentido volver a elegirla — por eso el color
            // de deshabilitado es opaco (si no, se vería apagada).
            bool esPisoActual = piso == GrillaGenerador.PisoActual;
            Button componenteBoton = boton.GetComponent<Button>();
            Tema.AplicarEstiloBoton(componenteBoton, esPisoActual ? Tema.EstiloBoton.Primario : Tema.EstiloBoton.Fantasma, 8);
            componenteBoton.interactable = !esPisoActual;
            ColorBlock colores = componenteBoton.colors;
            colores.disabledColor = Color.white;
            componenteBoton.colors = colores;

            TMP_Text texto = textoGO.GetComponent<TMP_Text>();
            string nombre = piso == 0 ? "PB" : $"Piso {piso}";
            texto.color = esPisoActual ? Tema.TextoSobreColor : Tema.TextoSecundario;

            // RF-20: un piso con advertencias lleva un punto rojo adelante,
            // así se ve que hay algo para corregir en un piso que no se está
            // mirando.
            ValidadorDiseno.Resultado validacion = ValidadorDiseno.UltimoResultado;
            bool tieneAdvertencias = validacion != null && validacion.PisosConAdvertencias.Contains(piso);
            string colorPunto = Tema.HexDe(esPisoActual ? Tema.Hex("#FFC2CD") : Tema.Peligro);
            texto.text = tieneAdvertencias ? $"<color={colorPunto}>●</color> {nombre}" : nombre;

            componenteBoton.onClick.AddListener(() => GrillaGenerador.MostrarPiso(piso));
        }

        // Eliminar un piso borra todas sus piezas, así que se pide
        // confirmación con el mismo menú que usan las piezas con clic derecho.
        private void ConfirmarEliminarPiso()
        {
            int piso = GrillaGenerador.PisoActual;
            Vector2 posicionPantalla = RectTransformUtility.WorldToScreenPoint(null, botonEliminarPiso.transform.position);

            MenuContextual.Mostrar(botonEliminarPiso, posicionPantalla,
                new MenuContextual.Opcion($"Eliminar {GrillaGenerador.NombrePiso(piso)} y sus piezas", () => GrillaGenerador.EliminarPiso(piso), esDestructiva: true),
                new MenuContextual.Opcion("Cancelar", () => { }));
        }
    }
}
