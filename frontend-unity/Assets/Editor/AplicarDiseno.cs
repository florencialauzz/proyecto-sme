using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sme.Grid;
using Sme.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Herramienta de editor que armó el diseño visual de las cinco escenas
// (menú SME > Aplicar diseño a todas las escenas). Reutiliza los objetos que
// ya existían — mismos GameObjects, mismas referencias en el Inspector y
// mismos eventos — y solo cambia su estilo, su posición y a qué contenedor
// pertenecen; lo nuevo (barras, tarjetas, títulos, recuadros de error) lo
// crea al lado.
//
// Los colores, fuentes y formas salen de Sme.UI.Tema, igual que la UI que se
// arma por código, así todo el sistema se ve igual.
//
// Cuándo correrla: después de mergear cambios de escena de otra rama que
// todavía tengan el diseño viejo, para volver a aplicarlo sobre ellas. Pisa
// los ajustes visuales hechos a mano en el Inspector sobre los objetos que
// toca, así que si se retoca algo a mano conviene retocarlo también acá.
public static class AplicarDiseno
{
    private const string CarpetaSprites = "Assets/Resources/UI";
    private const string CarpetaFuentes = "Assets/Resources/Fuentes";
    private const string CarpetaTtf = "Assets/Fuentes";

    // Lo que se ve en pantalla en una resolución de referencia de 1920x1080;
    // el CanvasScaler lo escala al tamaño real de la ventana.
    private static readonly Vector2 ResolucionReferencia = new Vector2(1920f, 1080f);

    private const float AltoBarraSuperior = 76f;
    private const float AltoCampo = 52f;
    private const float AltoBoton = 50f;

    [MenuItem("SME/Aplicar diseño a todas las escenas")]
    public static void AplicarTodo()
    {
        GenerarSprites();
        GenerarFuentes();
        DisenarPrefabs();
        DisenarEscena("Auth", DisenarAuth);
        DisenarEscena("Inicio", DisenarInicio);
        DisenarEscena("Configuracion", DisenarConfiguracion);
        DisenarEscena("Editor", DisenarEditor);
        DisenarEscena("Resultados", DisenarResultados);
        AssetDatabase.SaveAssets();
        Debug.Log("[AplicarDiseno] Listo.");
    }

    private static void DisenarEscena(string nombre, Action<Transform> disenar)
    {
        Scene escena = EditorSceneManager.OpenScene($"Assets/Scenes/{nombre}.unity", OpenSceneMode.Single);
        Canvas canvas = escena.GetRootGameObjects().Select(go => go.GetComponent<Canvas>()).First(c => c != null);

        CanvasScaler escalador = canvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = ResolucionReferencia;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        Camera camara = escena.GetRootGameObjects().Select(go => go.GetComponent<Camera>()).First(c => c != null);
        camara.backgroundColor = Tema.Fondo;

        // Fondo de toda la pantalla, detrás de todo lo demás.
        GameObject fondo = Hijo(canvas.transform, "Fondo", typeof(Image));
        fondo.transform.SetAsFirstSibling();
        Tema.Estirar(Rect(fondo));
        Image imagenFondo = fondo.GetComponent<Image>();
        imagenFondo.color = Tema.Fondo;
        imagenFondo.raycastTarget = false;

        disenar(canvas.transform);

        // Cualquier texto que haya quedado con la fuente vieja pasa a Inter.
        foreach (TMP_Text texto in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (texto.font == null || texto.font.name.StartsWith("LiberationSans"))
            {
                texto.font = Tema.FuenteTexto;
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // =====================================================================
    // Auth: portada a la izquierda, formularios a la derecha.
    // =====================================================================

    private static void DisenarAuth(Transform canvas)
    {
        CrearPortada(canvas);

        GameObject zona = Hijo(canvas, "ZonaFormularios", typeof(Image));
        RectTransform zonaRect = Rect(zona);
        zonaRect.anchorMin = new Vector2(0.54f, 0f);
        zonaRect.anchorMax = Vector2.one;
        zonaRect.offsetMin = zonaRect.offsetMax = Vector2.zero;
        zona.GetComponent<Image>().color = Tema.Superficie;

        Transform login = Buscar(canvas, "PanelLogin");
        Transform registro = Buscar(canvas, "PanelRegistro");
        Transform recuperar = Buscar(canvas, "PanelRecuperarContrasena");
        foreach (Transform panel in new[] { login, registro, recuperar })
        {
            panel.SetParent(zona.transform, false);
            RectTransform rect = Rect(panel);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(460f, 0f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            Vertical(panel.gameObject, 0, 0, 14f, TextAnchor.MiddleCenter);
        }

        // --- Iniciar sesión ---
        Transform tituloLogin = Titulo(login, "Titulo", "Iniciá sesión", 38f);
        Transform subLogin = Parrafo(login, "Subtitulo", "Ingresá con tu usuario para seguir trabajando en tus proyectos.");
        Transform grupoUsuario = Grupo(login, "GrupoUsuario", "Nombre de usuario", Buscar(login, "CampoNombreUsuario"), "Tu nombre de usuario");
        Transform grupoContrasena = Grupo(login, "GrupoContrasena", "Contraseña", Buscar(login, "CampoContrasena"), "Tu contraseña");
        Transform olvido = Buscar(login, "BotonIrARecuperarContrasena");
        Boton(olvido, Tema.EstiloBoton.Fantasma, "¿Olvidaste tu contraseña?", 34f);
        AlinearTextoBoton(olvido, TextAlignmentOptions.Right, 4f);
        Transform avisoLogin = CrearAvisoError(login, "PanelLogin", login.GetComponent<LoginScreen>());
        Transform iniciar = Buscar(login, "BotonIniciarSesion");
        Boton(iniciar, Tema.EstiloBoton.Primario, "Iniciar sesión", 54f);
        Transform separadorLogin = Separador(login, "Separador", "¿No tenés cuenta?");
        Transform irARegistro = Buscar(login, "BotonIrARegistro");
        Boton(irARegistro, Tema.EstiloBoton.Secundario, "Crear una cuenta nueva", AltoBoton);
        Ordenar(login, tituloLogin, subLogin, Espacio(login, "Espacio", 10f), grupoUsuario, grupoContrasena,
            olvido, avisoLogin, iniciar, separadorLogin, irARegistro);

        // --- Registro ---
        Transform tituloRegistro = Titulo(registro, "Titulo", "Creá tu cuenta", 38f);
        Transform subRegistro = Parrafo(registro, "Subtitulo", "Registrate para empezar a diseñar y simular estacionamientos.");
        Transform gUsuario = Grupo(registro, "GrupoUsuario", "Nombre de usuario", Buscar(registro, "CampoNombreUsuario"), "Elegí un nombre de usuario");
        Transform gContrasena = Grupo(registro, "GrupoContrasena", "Contraseña", Buscar(registro, "CampoContrasena"), "Elegí una contraseña");
        Transform gConfirmacion = Grupo(registro, "GrupoConfirmacion", "Repetí la contraseña", Buscar(registro, "CampoConfirmacionContrasena"), "Repetí la contraseña");
        Object.DestroyImmediate(Buscar(registro, "LabelPreguntaSeguridad").gameObject);
        Transform gPregunta = Grupo(registro, "GrupoPregunta", "Pregunta de seguridad", Buscar(registro, "PreguntaSeguridad"), null);
        Transform gRespuesta = Grupo(registro, "GrupoRespuesta", "Respuesta", Buscar(registro, "CampoRespuestaSeguridad"), "La vas a necesitar si olvidás tu contraseña");
        Transform avisoRegistro = CrearAvisoError(registro, "PanelRegistro", registro.GetComponent<RegistroScreen>());
        Transform registrar = Buscar(registro, "BotonRegistrar");
        Boton(registrar, Tema.EstiloBoton.Primario, "Crear cuenta", 54f);
        Transform separadorRegistro = Separador(registro, "Separador", "¿Ya tenés cuenta?");
        Transform irALogin = Buscar(registro, "BotonIrALogin");
        Boton(irALogin, Tema.EstiloBoton.Secundario, "Iniciar sesión", AltoBoton);
        Ordenar(registro, tituloRegistro, subRegistro, Espacio(registro, "Espacio", 4f), gUsuario, gContrasena, gConfirmacion,
            gPregunta, gRespuesta, avisoRegistro, registrar, separadorRegistro, irALogin);
        Vertical(registro.gameObject, 0, 0, 12f, TextAnchor.MiddleCenter);

        // --- Recuperar contraseña: dos pasos (PanelUsuario y PanelRespuesta) ---
        Transform tituloRecuperar = Titulo(recuperar, "Titulo", "Recuperá tu contraseña", 38f);
        Transform subRecuperar = Parrafo(recuperar, "Subtitulo", "Respondé tu pregunta de seguridad para elegir una contraseña nueva.");

        Transform pasoUsuario = Buscar(recuperar, "PanelUsuario");
        pasoUsuario.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        Vertical(pasoUsuario.gameObject, 0, 0, 14f, TextAnchor.UpperCenter);
        Transform gUsuarioRec = Grupo(pasoUsuario, "GrupoUsuario", "Nombre de usuario", Buscar(pasoUsuario, "CampoNombreUsuario"), "Tu nombre de usuario");
        Transform continuar = Buscar(pasoUsuario, "BotonContinuar");
        Boton(continuar, Tema.EstiloBoton.Primario, "Continuar", 54f);
        Ordenar(pasoUsuario, gUsuarioRec, continuar);

        Transform pasoRespuesta = Buscar(recuperar, "PanelRespuesta");
        pasoRespuesta.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        Vertical(pasoRespuesta.gameObject, 0, 0, 14f, TextAnchor.UpperCenter);
        Transform tarjetaPregunta = TarjetaPreguntaSeguridad(pasoRespuesta, Buscar(pasoRespuesta, "TextoPreguntaSeguridad"));
        Transform gRespuestaRec = Grupo(pasoRespuesta, "GrupoRespuesta", "Respuesta", Buscar(pasoRespuesta, "CampoRespuestaSeguridad"), "Tu respuesta");
        Transform gNueva = Grupo(pasoRespuesta, "GrupoNueva", "Contraseña nueva", Buscar(pasoRespuesta, "CampoNuevaContrasena"), "Elegí una contraseña nueva");
        Transform gConfNueva = Grupo(pasoRespuesta, "GrupoConfirmacion", "Repetí la contraseña nueva", Buscar(pasoRespuesta, "CampoConfirmacionNuevaContrasena"), "Repetí la contraseña nueva");
        Transform confirmar = Buscar(pasoRespuesta, "BotonRecuperar");
        Boton(confirmar, Tema.EstiloBoton.Primario, "Cambiar contraseña", 54f);
        Ordenar(pasoRespuesta, tarjetaPregunta, gRespuestaRec, gNueva, gConfNueva, confirmar);

        Transform avisoRecuperar = CrearAvisoError(recuperar, "PanelRecuperarContrasena", recuperar.GetComponent<RecuperarContrasenaScreen>());

        // El botón Volver estaba dentro del primer paso y apagaba ese paso en
        // vez del panel entero (quedaba el panel prendido detrás del login).
        // Ahora está abajo de los dos pasos y apaga el panel.
        Transform volver = Buscar(pasoUsuario, "BotonVolver");
        volver.SetParent(recuperar, false);
        Boton(volver, Tema.EstiloBoton.Fantasma, "←  Volver a iniciar sesión", 44f);
        CambiarDestinoSetActive(volver.GetComponent<Button>(), pasoUsuario.gameObject, recuperar.gameObject);

        Ordenar(recuperar, tituloRecuperar, subRecuperar, Espacio(recuperar, "Espacio", 6f), pasoUsuario, pasoRespuesta, avisoRecuperar, volver);
        AjustarAlto(pasoUsuario, -1f);
        AjustarAlto(pasoRespuesta, -1f);
    }

    // La portada: degradado de marca, el nombre del sistema y una vista
    // previa de un estacionamiento armada con los mismos sprites del editor.
    private static void CrearPortada(Transform canvas)
    {
        GameObject portada = Hijo(canvas, "Portada", typeof(Image));
        portada.transform.SetSiblingIndex(1);
        RectTransform rect = Rect(portada);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0.54f, 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image degradado = portada.GetComponent<Image>();
        degradado.sprite = SpriteUI("Degradado");
        degradado.color = Color.white;
        degradado.raycastTarget = false;

        Brillo(portada.transform, "BrilloSuperior", new Vector2(0.15f, 0.95f), 900f, new Color(1f, 1f, 1f, 0.22f));
        Brillo(portada.transform, "BrilloInferior", new Vector2(0.95f, 0.05f), 1000f, new Color(0.4f, 1f, 1f, 0.35f));

        GameObject puntos = Hijo(portada.transform, "Puntos", typeof(Image));
        Tema.Estirar(Rect(puntos));
        Image imagenPuntos = puntos.GetComponent<Image>();
        imagenPuntos.sprite = SpriteUI("Puntos");
        imagenPuntos.type = Image.Type.Tiled;
        imagenPuntos.color = new Color(1f, 1f, 1f, 0.12f);
        imagenPuntos.raycastTarget = false;

        // Marca arriba a la izquierda.
        Transform marca = Marca(portada.transform, true);
        Anclar(Rect(marca), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -56f), new Vector2(320f, 48f));

        GameObject contenido = Hijo(portada.transform, "Contenido");
        Anclar(Rect(contenido), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 120f), new Vector2(820f, 520f), new Vector2(0f, 0.5f));
        // Sin forzar el ancho: la etiqueta de arriba es una píldora angosta.
        Vertical(contenido, 0, 0, 22f, TextAnchor.MiddleLeft).childForceExpandWidth = false;

        Transform etiqueta = Pildora(contenido.transform, "Etiqueta", "PROYECTO INTEGRADOR  ·  UDE",
            new Color(1f, 1f, 1f, 0.16f), Color.white, 13f);
        LayoutElement etiquetaLayout = etiqueta.GetComponent<LayoutElement>();
        etiquetaLayout.preferredWidth = 290f;

        TMP_Text titulo = TextoEn(contenido.transform, "Titulo", "Sistema de Modelado\nde Estacionamiento",
            Tema.FuenteTitulo, 64f, Color.white, TextAlignmentOptions.TopLeft);
        titulo.lineSpacing = -24f;
        Layout(titulo.gameObject).flexibleWidth = 1f;
        TMP_Text bajada = TextoEn(contenido.transform, "Bajada",
            "Diseñá estacionamientos comerciales con un editor de arrastrar y soltar, simulá su flujo vehicular y verificá el cumplimiento normativo, todo en un mismo lugar.",
            Tema.FuenteTexto, 21f, new Color(1f, 1f, 1f, 0.88f), TextAlignmentOptions.TopLeft);
        bajada.lineSpacing = 8f;
        LayoutElement bajadaLayout = Layout(bajada.gameObject);
        bajadaLayout.preferredWidth = 700f;

        GameObject caracteristicas = Hijo(contenido.transform, "Caracteristicas");
        HorizontalLayoutGroup filaCaracteristicas = Horizontal(caracteristicas, 0, 0, 12f, TextAnchor.MiddleLeft);
        filaCaracteristicas.childControlWidth = true;
        Layout(caracteristicas).preferredHeight = 44f;
        Layout(caracteristicas).flexibleWidth = 1f;
        string[] textos = { "Editor 2D", "Simulación de ocupación", "Validación normativa" };
        float[] anchos = { 150f, 250f, 230f };
        for (int i = 0; i < textos.Length; i++)
        {
            Transform pildora = Pildora(caracteristicas.transform, $"Caracteristica{i}", "<color=#7FF3FF>●</color>  " + textos[i],
                new Color(1f, 1f, 1f, 0.14f), Color.white, 16f);
            pildora.GetComponent<LayoutElement>().preferredWidth = anchos[i];
            pildora.GetComponent<LayoutElement>().preferredHeight = 44f;
        }
        Ordenar(contenido.transform, etiqueta, titulo.transform, bajada.transform, caracteristicas.transform);

        VistaPreviaEstacionamiento(portada.transform);

        TMP_Text pie = TextoEn(portada.transform, "Pie", "Luca Guillén  ·  Florencia Láuz  ·  Tutor: Ing. Emiliano Adinolfi",
            Tema.FuenteTexto, 14f, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.BottomLeft);
        Anclar(pie.rectTransform, Vector2.zero, Vector2.zero, new Vector2(72f, 40f), new Vector2(800f, 24f));
    }

    // Una "tarjeta de vidrio" con una fila de plazas, algunas ocupadas, y la
    // calle de acceso: muestra de qué se trata el sistema sin palabras.
    private static void VistaPreviaEstacionamiento(Transform portada)
    {
        const float celda = 58f;
        const int columnas = 8;

        GameObject tarjeta = Hijo(portada, "VistaPrevia", typeof(Image));
        RectTransform rect = Rect(tarjeta);
        float ancho = columnas * celda + 48f;
        float alto = celda * 3f + 48f;
        Anclar(rect, Vector2.zero, Vector2.zero, new Vector2(72f, 100f), new Vector2(ancho, alto));
        Image vidrio = tarjeta.GetComponent<Image>();
        vidrio.sprite = Tema.Redondeado(24);
        vidrio.type = Image.Type.Sliced;
        vidrio.color = new Color(1f, 1f, 1f, 0.14f);
        vidrio.raycastTarget = false;

        Sprite plaza = SpritePieza("plaza");
        Sprite plazaAccesible = SpritePieza("plazaAccesible");
        Sprite auto = SpritePieza("auto");
        Sprite calle = SpritePieza("calle");
        Sprite entrada = SpritePieza("entrada");
        Sprite salida = SpritePieza("salida");
        bool[] ocupadas = { true, false, true, true, false, true, false, true };

        for (int i = 0; i < columnas; i++)
        {
            float x = 24f + celda * i;
            Sprite spritePlaza = i == 1 ? plazaAccesible : plaza;
            Image imagenPlaza = ImagenEn(tarjeta.transform, $"Plaza{i}", spritePlaza);
            Anclar(imagenPlaza.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -24f), new Vector2(celda, celda * 2f));
            if (ocupadas[i])
            {
                Image imagenAuto = ImagenEn(imagenPlaza.transform, "Auto", auto);
                imagenAuto.preserveAspect = true;
                Anclar(imagenAuto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(celda * 0.62f, celda * 1.5f), new Vector2(0.5f, 0.5f));
            }

            // Fila de la calle: entrada a la izquierda, salida a la derecha,
            // rotadas para que el tránsito corra horizontal.
            Sprite spriteCalle = i == 0 ? entrada : i == columnas - 1 ? salida : calle;
            Image imagenCalle = ImagenEn(tarjeta.transform, $"Calle{i}", spriteCalle);
            Anclar(imagenCalle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x + celda / 2f, -24f - celda * 2.5f), new Vector2(celda, celda), new Vector2(0.5f, 0.5f));
            imagenCalle.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
        }
    }

    // Recuadro destacado con la pregunta de seguridad del usuario (paso 2 de
    // recuperar contraseña).
    private static Transform TarjetaPreguntaSeguridad(Transform padre, Transform textoPregunta)
    {
        GameObject tarjeta = Hijo(padre, "TarjetaPregunta", typeof(Image));
        Image fondo = tarjeta.GetComponent<Image>();
        fondo.sprite = Tema.Redondeado(12);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.PrimarioSuave;
        Vertical(tarjeta, 18, 14, 4f, TextAnchor.UpperLeft);
        Transform etiqueta = Etiqueta(tarjeta.transform, "Etiqueta", "TU PREGUNTA DE SEGURIDAD", Tema.Primario, 12f);
        textoPregunta.SetParent(tarjeta.transform, false);
        EstiloTexto(textoPregunta.GetComponent<TMP_Text>(), Tema.FuenteSubtitulo, 18f, Tema.Texto, TextAlignmentOptions.TopLeft);
        textoPregunta.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
        Ordenar(tarjeta.transform, etiqueta, textoPregunta);
        return tarjeta.transform;
    }

    // =====================================================================
    // Inicio: barra lateral de perfil y área de proyectos.
    // =====================================================================

    private const float AnchoBarraLateral = 340f;

    private static void DisenarInicio(Transform canvas)
    {
        Transform panelInicio = Buscar(canvas, "PanelInicio");
        InicioScreen pantalla = panelInicio.GetComponent<InicioScreen>();
        Tema.Estirar(Rect(panelInicio));
        panelInicio.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        // --- Barra lateral: marca, perfil y configuración de la cuenta ---
        GameObject lateral = Hijo(panelInicio, "BarraLateral", typeof(Image));
        RectTransform lateralRect = Rect(lateral);
        lateralRect.anchorMin = Vector2.zero;
        lateralRect.anchorMax = new Vector2(0f, 1f);
        lateralRect.pivot = new Vector2(0f, 0.5f);
        lateralRect.sizeDelta = new Vector2(AnchoBarraLateral, 0f);
        lateralRect.anchoredPosition = Vector2.zero;
        lateral.GetComponent<Image>().color = Tema.Superficie;
        Vertical(lateral, 28, 32, 12f, TextAnchor.UpperLeft);
        LineaBorde(lateral.transform, "BordeDerecho", false);

        Transform marca = Marca(lateral.transform, false);
        Layout(marca.gameObject).preferredHeight = 44f;

        Transform etiquetaPerfil = Etiqueta(lateral.transform, "EtiquetaPerfil", "PERFIL", Tema.TextoTenue, 12f);
        Transform tarjetaPerfil = TarjetaPerfil(lateral.transform, pantalla);

        Transform etiquetaCuenta = Etiqueta(lateral.transform, "EtiquetaCuenta", "CONFIGURACIÓN DE LA CUENTA", Tema.TextoTenue, 12f);
        Transform cambiar = Buscar(panelInicio, "BotonIrACambiarContrasena");
        Boton(cambiar, Tema.EstiloBoton.Secundario, "Cambiar contraseña", 48f);
        AlinearTextoBoton(cambiar, TextAlignmentOptions.Left, 18f);
        Transform cerrar = Buscar(panelInicio, "BotonCerrarSesion");
        Boton(cerrar, Tema.EstiloBoton.Secundario, "Cerrar sesión", 48f);
        AlinearTextoBoton(cerrar, TextAlignmentOptions.Left, 18f);

        Transform relleno = Espacio(lateral.transform, "Relleno", 0f);
        relleno.GetComponent<LayoutElement>().flexibleHeight = 1f;

        Transform etiquetaPeligro = Etiqueta(lateral.transform, "EtiquetaZonaPeligro", "ZONA DE PELIGRO", Tema.TextoTenue, 12f);
        Transform eliminarCuenta = Buscar(panelInicio, "BotonIrAEliminarCuenta");
        Boton(eliminarCuenta, Tema.EstiloBoton.PeligroSuave, "Eliminar cuenta", 48f);
        AlinearTextoBoton(eliminarCuenta, TextAlignmentOptions.Left, 18f);
        Transform avisoEliminar = Parrafo(lateral.transform, "AvisoEliminar", "Borra tu cuenta y todos tus proyectos.", 14f);

        Ordenar(lateral.transform, marca, Espacio(lateral.transform, "Espacio1", 20f), etiquetaPerfil, tarjetaPerfil,
            Espacio(lateral.transform, "Espacio2", 16f), etiquetaCuenta, cambiar, cerrar, relleno, etiquetaPeligro, eliminarCuenta, avisoEliminar);
        // Los modales se abren encima de Inicio, que queda visible detrás
        // del velo oscuro: los botones ya no apagan PanelInicio.
        QuitarSetActive(cambiar.GetComponent<Button>(), panelInicio.gameObject);
        QuitarSetActive(eliminarCuenta.GetComponent<Button>(), panelInicio.gameObject);

        // --- Contenido: encabezado, nuevo proyecto y proyectos guardados ---
        GameObject contenido = Hijo(panelInicio, "Contenido");
        RectTransform contenidoRect = Rect(contenido);
        contenidoRect.anchorMin = Vector2.zero;
        contenidoRect.anchorMax = Vector2.one;
        contenidoRect.offsetMin = new Vector2(AnchoBarraLateral, 0f);
        contenidoRect.offsetMax = Vector2.zero;
        VerticalLayoutGroup columna = Vertical(contenido, 64, 52, 24f, TextAnchor.UpperLeft);
        columna.padding.bottom = 44;

        GameObject encabezado = Hijo(contenido.transform, "Encabezado");
        Vertical(encabezado, 0, 0, 6f, TextAnchor.UpperLeft);
        Transform titulo = Titulo(encabezado.transform, "Titulo", "Mis proyectos", 42f);
        Transform bajada = Parrafo(encabezado.transform, "Bajada", "Creá un proyecto nuevo o seguí trabajando en uno que ya tengas guardado.", 18f);
        Ordenar(encabezado.transform, titulo, bajada);

        Transform aviso = CrearAvisoError(contenido.transform, "PanelInicio", pantalla, panelInicio);

        // Nuevo proyecto
        Transform tarjetaNuevo = Tarjeta(contenido.transform, "TarjetaNuevoProyecto", 28, 24, 16f);
        Transform tituloNuevo = Subtitulo(tarjetaNuevo, "Titulo", "Nuevo proyecto");
        GameObject filaNuevo = Hijo(tarjetaNuevo, "Fila");
        Horizontal(filaNuevo, 0, 0, 12f, TextAnchor.MiddleLeft).childControlWidth = true;
        Layout(filaNuevo).preferredHeight = AltoCampo;
        Transform campoNombre = Buscar(panelInicio, "CampoNombreProyecto");
        Campo(campoNombre, "Nombre del proyecto, por ejemplo \"Centro comercial — nivel 1\"");
        campoNombre.SetParent(filaNuevo.transform, false);
        Layout(campoNombre.gameObject).flexibleWidth = 1f;
        Transform crear = Buscar(panelInicio, "BotonCrearProyecto");
        crear.SetParent(filaNuevo.transform, false);
        Boton(crear, Tema.EstiloBoton.Primario, "+  Crear proyecto", AltoCampo, 220f);
        Ordenar(filaNuevo.transform, campoNombre, crear);
        Ordenar(tarjetaNuevo, tituloNuevo, filaNuevo.transform);

        // Proyectos guardados
        Transform tarjetaLista = Tarjeta(contenido.transform, "TarjetaProyectos", 28, 24, 16f);
        Layout(tarjetaLista.gameObject).flexibleHeight = 1f;
        GameObject cabecera = Hijo(tarjetaLista, "Cabecera");
        Horizontal(cabecera, 0, 0, 12f, TextAnchor.MiddleLeft).childControlWidth = true;
        Layout(cabecera).preferredHeight = 32f;
        Transform tituloLista = Buscar(panelInicio, "TituloPoyectos");
        tituloLista.SetParent(cabecera.transform, false);
        EstiloTexto(tituloLista.GetComponent<TMP_Text>(), Tema.FuenteSubtitulo, Tema.TamanioSubtitulo, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        tituloLista.GetComponent<TMP_Text>().text = "Proyectos guardados";
        Layout(tituloLista.gameObject).flexibleWidth = 1f;
        Transform ayudaSeleccion = Parrafo(cabecera.transform, "AyudaSeleccion", "Seleccioná uno para abrirlo, o dos para compararlos. Clic derecho para cambiarle el nombre.", 15f);
        ayudaSeleccion.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;
        ayudaSeleccion.GetComponent<TMP_Text>().color = Tema.TextoTenue;
        Ordenar(cabecera.transform, tituloLista, ayudaSeleccion);

        Transform lista = ListaDesplazable(tarjetaLista, "Lista", Buscar(panelInicio, "ContenedorProyectos"));
        Layout(lista.gameObject).flexibleHeight = 1f;
        Transform sinProyectos = Buscar(panelInicio, "TextoSinProyectos");
        sinProyectos.SetParent(lista, false);
        Layout(sinProyectos.gameObject).ignoreLayout = true;
        Anclar(Rect(sinProyectos), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 80f), new Vector2(0.5f, 0.5f));
        TMP_Text textoSinProyectos = sinProyectos.GetComponent<TMP_Text>();
        EstiloTexto(textoSinProyectos, Tema.FuenteTexto, 17f, Tema.TextoSecundario, TextAlignmentOptions.Center);
        textoSinProyectos.text = $"<size=20><color={Tema.HexDe(Tema.Texto)}><b>Todavía no tenés proyectos</b></color></size>\nCreá el primero desde la tarjeta de arriba.";

        GameObject acciones = Hijo(tarjetaLista, "Acciones");
        Horizontal(acciones, 0, 0, 10f, TextAnchor.MiddleLeft).childControlWidth = true;
        Layout(acciones).preferredHeight = 48f;
        Transform abrir = Buscar(panelInicio, "BotonAbrirProyect");
        Transform verResultados = Buscar(panelInicio, "BotonVerResultados");
        Transform comparar = Buscar(panelInicio, "BotonComparar");
        Transform duplicar = Buscar(panelInicio, "BotonDuplicar");
        Transform eliminar = Buscar(panelInicio, "BotonEliminar");
        foreach (Transform boton in new[] { abrir, verResultados, comparar, duplicar, eliminar })
        {
            boton.SetParent(acciones.transform, false);
        }
        Boton(abrir, Tema.EstiloBoton.Primario, "Abrir proyecto", 48f, 180f);
        Boton(verResultados, Tema.EstiloBoton.Secundario, "Ver resultados", 48f, 170f);
        Boton(comparar, Tema.EstiloBoton.Secundario, "Comparar", 48f, 130f);
        Boton(duplicar, Tema.EstiloBoton.Secundario, "Duplicar", 48f, 120f);
        Boton(eliminar, Tema.EstiloBoton.PeligroSuave, "Eliminar", 48f, 120f);
        Transform rellenoAcciones = Espacio(acciones.transform, "Relleno", 0f);
        rellenoAcciones.GetComponent<LayoutElement>().flexibleWidth = 1f;
        Ordenar(acciones.transform, abrir, verResultados, comparar, duplicar, rellenoAcciones, eliminar);

        Ordenar(tarjetaLista, cabecera.transform, lista, acciones.transform);
        Ordenar(contenido.transform, encabezado.transform, aviso, tarjetaNuevo, tarjetaLista);

        DisenarModalCambiarContrasena(canvas);
        DisenarModalEliminarCuenta(canvas);
        DisenarModalConfirmarEliminar(canvas);
        DisenarModalComparacion(canvas);
        DisenarModalRenombrar(canvas, pantalla);
    }

    // Modal nuevo (no existía en la escena): cambiar el nombre de un proyecto.
    private static void DisenarModalRenombrar(Transform canvas, InicioScreen pantalla)
    {
        GameObject panel = Hijo(canvas, "PanelRenombrar", typeof(Image));
        Transform tarjeta = PrepararModal(panel.transform, 500f);

        Transform titulo = Titulo(tarjeta, "Titulo", "Cambiar nombre", 28f);
        Transform bajada = Parrafo(tarjeta, "Bajada", "Elegí un nombre nuevo para el proyecto.");
        Transform campo = NuevoCampo(tarjeta, "CampoNuevoNombre");
        Transform grupo = Grupo(tarjeta, "GrupoNombre", "Nombre del proyecto", campo, "Nombre del proyecto");

        // El texto de error lo crea acá: CrearAvisoError envuelve uno existente.
        TextoEn(tarjeta, "TextoError", string.Empty, Tema.FuenteTextoFuerte, 15f, Tema.PeligroOscuro, TextAlignmentOptions.TopLeft);
        Transform aviso = CrearAvisoError(tarjeta, "PanelRenombrar", pantalla, campo: "avisoErrorRenombrar");

        Transform cancelar = NuevoBoton(tarjeta, "BotonCancelarRenombrar");
        Transform confirmar = NuevoBoton(tarjeta, "BotonConfirmarRenombrar");
        Transform fila = FilaBotones(tarjeta, cancelar, confirmar);
        Boton(cancelar, Tema.EstiloBoton.Secundario, "Cancelar", AltoBoton);
        Boton(confirmar, Tema.EstiloBoton.Primario, "Guardar nombre", AltoBoton);
        Ordenar(tarjeta, titulo, bajada, Espacio(tarjeta, "Espacio", 4f), grupo, aviso, Espacio(tarjeta, "Espacio2", 4f), fila);

        Asignar(pantalla, "panelRenombrar", panel);
        Asignar(pantalla, "campoNuevoNombre", campo.GetComponent<TMP_InputField>());
        Asignar(pantalla, "botonConfirmarRenombrar", confirmar.GetComponent<Button>());
        Asignar(pantalla, "botonCancelarRenombrar", cancelar.GetComponent<Button>());
        panel.SetActive(false);
    }

    private static Transform TarjetaPerfil(Transform padre, InicioScreen pantalla)
    {
        GameObject tarjeta = Hijo(padre, "TarjetaPerfil", typeof(Image));
        Image fondo = tarjeta.GetComponent<Image>();
        fondo.sprite = Tema.Redondeado(14);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.SuperficieSuave;
        HorizontalLayoutGroup fila = Horizontal(tarjeta, 16, 16, 14f, TextAnchor.MiddleLeft);
        fila.childControlWidth = true;
        Layout(tarjeta).preferredHeight = 84f;

        GameObject avatar = Hijo(tarjeta.transform, "Avatar", typeof(Image));
        Image imagenAvatar = avatar.GetComponent<Image>();
        imagenAvatar.sprite = SpriteUI("DegradadoCirculo");
        imagenAvatar.color = Color.white;
        LayoutElement avatarLayout = Layout(avatar);
        avatarLayout.preferredWidth = avatarLayout.minWidth = 52f;
        avatarLayout.preferredHeight = 52f;
        TMP_Text inicial = TextoEn(avatar.transform, "Inicial", "U", Tema.FuenteTitulo, 22f, Color.white, TextAlignmentOptions.Center);
        Tema.Estirar(inicial.rectTransform);
        inicial.margin = new Vector4(0f, 3f, 0f, 0f);

        GameObject datos = Hijo(tarjeta.transform, "Datos");
        Vertical(datos, 0, 0, 2f, TextAnchor.MiddleLeft);
        Layout(datos).flexibleWidth = 1f;
        TMP_Text nombre = TextoEn(datos.transform, "NombreUsuario", "Usuario", Tema.FuenteSubtitulo, 18f, Tema.Texto, TextAlignmentOptions.Left);
        nombre.overflowMode = TextOverflowModes.Ellipsis;
        nombre.textWrappingMode = TextWrappingModes.NoWrap;
        TMP_Text estado = TextoEn(datos.transform, "Estado", $"<color={Tema.HexDe(Tema.Exito)}>●</color>  Sesión iniciada",
            Tema.FuenteTexto, 14f, Tema.TextoSecundario, TextAlignmentOptions.Left);
        Ordenar(datos.transform, nombre.transform, estado.transform);
        Ordenar(tarjeta.transform, avatar.transform, datos.transform);

        Asignar(pantalla, "textoNombreUsuario", nombre);
        Asignar(pantalla, "textoInicialUsuario", inicial);
        return tarjeta.transform;
    }

    private static void DisenarModalCambiarContrasena(Transform canvas)
    {
        Transform panel = Buscar(canvas, "PanelCambiarContrasena");
        Transform tarjeta = PrepararModal(panel, 500f);

        Transform titulo = Titulo(tarjeta, "Titulo", "Cambiar contraseña", 28f);
        Transform bajada = Parrafo(tarjeta, "Bajada", "Ingresá tu contraseña actual y elegí una nueva.");
        Transform gActual = Grupo(tarjeta, "GrupoActual", "Contraseña actual", Buscar(panel, "CampoContrasenaActual"), "Tu contraseña actual");
        Transform gNueva = Grupo(tarjeta, "GrupoNueva", "Contraseña nueva", Buscar(panel, "CampoContrasenaNueva"), "Elegí una contraseña nueva");
        Transform gConfirmacion = Grupo(tarjeta, "GrupoConfirmacion", "Repetí la contraseña nueva", Buscar(panel, "CampoConfirmacionContrasenaNueva"), "Repetí la contraseña nueva");
        Transform aviso = CrearAvisoError(tarjeta, "PanelCambiarContrasena", panel.GetComponent<CambiarContrasenaScreen>(), panel);
        Transform fila = FilaBotones(tarjeta, Buscar(panel, "BotonCancelar"), Buscar(panel, "BotonCambiarContrasena"));
        Boton(Buscar(fila, "BotonCancelar"), Tema.EstiloBoton.Secundario, "Cancelar", AltoBoton);
        Boton(Buscar(fila, "BotonCambiarContrasena"), Tema.EstiloBoton.Primario, "Guardar cambios", AltoBoton);
        Ordenar(tarjeta, titulo, bajada, Espacio(tarjeta, "Espacio", 4f), gActual, gNueva, gConfirmacion, aviso, Espacio(tarjeta, "Espacio2", 4f), fila);
    }

    private static void DisenarModalEliminarCuenta(Transform canvas)
    {
        Transform panel = Buscar(canvas, "PanelEliminarCuenta");
        Transform tarjeta = PrepararModal(panel, 500f);

        Transform icono = IconoAlerta(tarjeta, "Icono", "!", Tema.PeligroSuave, Tema.Peligro);
        Transform titulo = Titulo(tarjeta, "Titulo", "Eliminar cuenta", 28f);
        Transform bajada = Parrafo(tarjeta, "Bajada", "Esta acción es permanente: se borran tu cuenta y todos tus proyectos. Ingresá tu contraseña para confirmar.");
        Transform gContrasena = Grupo(tarjeta, "GrupoContrasena", "Contraseña", Buscar(panel, "CampoContrasena"), "Tu contraseña");
        Transform aviso = CrearAvisoError(tarjeta, "PanelEliminarCuenta", panel.GetComponent<EliminarCuentaScreen>(), panel);
        Transform botonEliminar = Buscar(panel, "BotonEliminarCuenta");
        botonEliminar.name = "BotonEliminarCuenta";
        Transform fila = FilaBotones(tarjeta, Buscar(panel, "BotonCancelar"), botonEliminar);
        Boton(Buscar(fila, "BotonCancelar"), Tema.EstiloBoton.Secundario, "Cancelar", AltoBoton);
        Boton(botonEliminar, Tema.EstiloBoton.Peligro, "Eliminar cuenta", AltoBoton);
        Ordenar(tarjeta, icono, titulo, bajada, Espacio(tarjeta, "Espacio", 4f), gContrasena, aviso, Espacio(tarjeta, "Espacio2", 4f), fila);

        // El botón cerraba el modal al hacer clic, antes de saber si la
        // contraseña era correcta: un error quedaba escondido. Ahora el
        // modal se queda abierto y EliminarCuentaScreen lleva a Auth si sale
        // bien.
        QuitarSetActive(botonEliminar.GetComponent<Button>(), panel.gameObject);
        QuitarSetActive(botonEliminar.GetComponent<Button>(), Buscar(canvas, "PanelInicio").gameObject);
    }

    private static void DisenarModalConfirmarEliminar(Transform canvas)
    {
        Transform panel = Buscar(canvas, "PanelConfirmarEliminar");
        Tema.Estirar(Rect(panel));
        panel.GetComponent<Image>().color = Tema.Velo;

        Transform tarjeta = Buscar(panel, "Image");
        tarjeta.name = "Tarjeta";
        EstiloTarjeta(tarjeta.gameObject, 20);
        Anclar(Rect(tarjeta), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 0f), new Vector2(0.5f, 0.5f));
        Vertical(tarjeta.gameObject, 36, 32, 14f, TextAnchor.UpperLeft);
        AjustarAlto(tarjeta, -1f);

        Transform icono = IconoAlerta(tarjeta, "Icono", "!", Tema.PeligroSuave, Tema.Peligro);
        Transform titulo = Titulo(tarjeta, "Titulo", "Eliminar proyecto", 28f);
        Transform texto = Buscar(tarjeta, "TextoConfirmarEliminar");
        EstiloTexto(texto.GetComponent<TMP_Text>(), Tema.FuenteTexto, 17f, Tema.TextoSecundario, TextAlignmentOptions.TopLeft);
        texto.GetComponent<TMP_Text>().lineSpacing = 6f;
        Transform fila = FilaBotones(tarjeta, Buscar(tarjeta, "BotonCancelarEliminar"), Buscar(tarjeta, "BotonConfirmarEliminar"));
        Boton(Buscar(fila, "BotonCancelarEliminar"), Tema.EstiloBoton.Secundario, "Cancelar", AltoBoton);
        Boton(Buscar(fila, "BotonConfirmarEliminar"), Tema.EstiloBoton.Peligro, "Eliminar", AltoBoton);
        Ordenar(tarjeta, icono, titulo, texto, Espacio(tarjeta, "Espacio", 6f), fila);
    }

    private static void DisenarModalComparacion(Transform canvas)
    {
        Transform raiz = Buscar(canvas, "ComparacionProyectos");
        raiz.SetAsLastSibling();
        Transform panel = Buscar(raiz, "PanelComparacion");
        Tema.Estirar(Rect(panel));
        panel.GetComponent<Image>().color = Tema.Velo;

        GameObject tarjeta = Hijo(panel, "Tarjeta", typeof(Image), typeof(Shadow));
        EstiloTarjeta(tarjeta, 24);
        Anclar(Rect(tarjeta), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 0f), new Vector2(0.5f, 0.5f));
        Vertical(tarjeta, 40, 36, 18f, TextAnchor.UpperLeft);
        AjustarAlto(tarjeta.transform, -1f);

        Transform titulo = Titulo(tarjeta.transform, "Titulo", "Comparación de resultados", 30f);
        Transform bajada = Parrafo(tarjeta.transform, "Bajada", "Indicadores de la simulación guardada de cada proyecto, uno al lado del otro.");

        GameObject columnas = Hijo(tarjeta.transform, "Columnas");
        HorizontalLayoutGroup filaColumnas = Horizontal(columnas, 0, 0, 20f, TextAnchor.UpperLeft);
        filaColumnas.childControlWidth = true;
        filaColumnas.childForceExpandWidth = true;
        string[] modales = { "ModalIzquierdo", "ModalDerecho" };
        string[] titulos = { "TituloIzquierdo", "TituloIzDerecho" };
        string[] cuerpos = { "CuerpoIzquierdo", "CuerpoDerecho" };
        Color[] acentos = { Tema.Primario, Tema.Acento };
        for (int i = 0; i < 2; i++)
        {
            Transform modal = Buscar(panel, modales[i]);
            modal.SetParent(columnas.transform, false);
            Image fondo = modal.GetComponent<Image>();
            fondo.sprite = Tema.Redondeado(16);
            fondo.type = Image.Type.Sliced;
            fondo.color = Tema.SuperficieSuave;
            Vertical(modal.gameObject, 28, 24, 14f, TextAnchor.UpperLeft);
            Layout(modal.gameObject).flexibleWidth = 1f;

            // Barrita de color arriba de cada columna: el layout estira el
            // contenedor a todo el ancho, la barra va anclada a la izquierda.
            Transform barra = Espacio(modal, "Acento", 5f);
            GameObject linea = Hijo(barra, "Linea", typeof(Image));
            Image imagenBarra = linea.GetComponent<Image>();
            imagenBarra.sprite = Tema.Redondeado(6);
            imagenBarra.type = Image.Type.Sliced;
            imagenBarra.color = acentos[i];
            Anclar(Rect(linea), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(48f, 5f), new Vector2(0f, 0.5f));
            Transform tituloModal = Buscar(modal, titulos[i]);
            EstiloTexto(tituloModal.GetComponent<TMP_Text>(), Tema.FuenteSubtitulo, 22f, Tema.Texto, TextAlignmentOptions.TopLeft);
            tituloModal.GetComponent<TMP_Text>().text = "Proyecto";
            Transform cuerpo = Buscar(modal, cuerpos[i]);
            EstiloTexto(cuerpo.GetComponent<TMP_Text>(), Tema.FuenteTexto, 18f, Tema.TextoSecundario, TextAlignmentOptions.TopLeft);
            cuerpo.GetComponent<TMP_Text>().lineSpacing = 22f;
            cuerpo.GetComponent<TMP_Text>().text = "Eficiencia: —\nDemanda: —\nSaturación: —\nPuntuación: —";
            Ordenar(modal, barra, tituloModal, cuerpo);
        }
        Ordenar(columnas.transform, Buscar(columnas.transform, "ModalIzquierdo"), Buscar(columnas.transform, "ModalDerecho"));

        Transform cerrar = Buscar(panel, "BotonCerrar");
        GameObject filaCerrar = Hijo(tarjeta.transform, "FilaCerrar");
        Horizontal(filaCerrar, 0, 0, 0f, TextAnchor.MiddleRight).childControlWidth = true;
        Layout(filaCerrar).preferredHeight = AltoBoton;
        cerrar.SetParent(filaCerrar.transform, false);
        Boton(cerrar, Tema.EstiloBoton.Primario, "Cerrar", AltoBoton, 160f);

        Ordenar(tarjeta.transform, titulo, bajada, columnas.transform, filaCerrar.transform);

        // PanelComparacion.Awake lo apaga igual; apagado en la escena no
        // tapa Inicio mientras se edita.
        panel.gameObject.SetActive(false);
    }

    // =====================================================================
    // Configuración: barra superior y formulario en una tarjeta centrada.
    // =====================================================================

    private static void DisenarConfiguracion(Transform canvas)
    {
        BarraSuperior(canvas, "Configuración de la simulación", "Paso previo al editor");

        Transform formulario = Buscar(canvas, "PanelFormulario");
        EstiloTarjeta(formulario.gameObject, 24);
        Anclar(Rect(formulario), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -AltoBarraSuperior / 2f), new Vector2(660f, 0f), new Vector2(0.5f, 0.5f));
        Vertical(formulario.gameObject, 44, 40, 16f, TextAnchor.UpperLeft);
        AjustarAlto(formulario, -1f);

        Transform titulo = Titulo(formulario, "Titulo", "Parámetros de la simulación", 30f);
        Transform bajada = Parrafo(formulario, "Bajada", "Definí cómo se va a usar el estacionamiento. Podés volver a cambiarlos desde el editor para probar otros escenarios.");

        Transform gPisos = GrupoConEtiqueta(formulario, "GrupoPisos", Buscar(formulario, "LabelCantidadPisos"), "Cantidad de pisos", Buscar(formulario, "InputCantidadPisos"), "Ej.: 2");
        Transform gFrecuencia = GrupoConEtiqueta(formulario, "GrupoFrecuencia", Buscar(formulario, "LabelFrecuenciaIngreso"), "Frecuencia de ingreso (vehículos por hora)", Buscar(formulario, "InputFrecuenciaIngreso"), "Ej.: 40");
        Transform gPermanencia = GrupoConEtiqueta(formulario, "GrupoPermanencia", Buscar(formulario, "LabelTiempoPermanencia"), "Tiempo de permanencia (minutos)", Buscar(formulario, "InputTiempoPermanencia"), "Ej.: 90");

        GameObject filaHorario = Hijo(formulario, "FilaHorario");
        HorizontalLayoutGroup horario = Horizontal(filaHorario, 0, 0, 16f, TextAnchor.UpperLeft);
        horario.childControlWidth = true;
        horario.childForceExpandWidth = true;
        Transform gInicio = GrupoConEtiqueta(filaHorario.transform, "GrupoHoraInicio", Buscar(formulario, "LabelHoraInicio"), "Hora de inicio", Buscar(formulario, "InputHoraInicio"), "08:00");
        Transform gFin = GrupoConEtiqueta(filaHorario.transform, "GrupoHoraFin", Buscar(formulario, "LabelHoraFin"), "Hora de fin", Buscar(formulario, "InputHoraFin"), "20:00");
        Ordenar(filaHorario.transform, gInicio, gFin);

        // Fluctuaciones: la casilla con su texto y el ícono "i" con la explicación.
        Transform filaFluctuaciones = Buscar(formulario, "FilaFluctuaciones");
        HorizontalLayoutGroup filaFluct = Horizontal(filaFluctuaciones.gameObject, 0, 0, 10f, TextAnchor.MiddleLeft);
        filaFluct.childControlWidth = true;
        Layout(filaFluctuaciones.gameObject).preferredHeight = 30f;
        Transform toggle = Buscar(filaFluctuaciones, "ToggleFluctuaciones");
        Casilla(toggle.GetComponent<Toggle>());
        TMP_Text textoToggle = Buscar(toggle, "Text (TMP)").GetComponent<TMP_Text>();
        EstiloTexto(textoToggle, Tema.FuenteTexto, 16f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        textoToggle.text = "Simular con variación aleatoria (±50%)";
        Anclar(textoToggle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        textoToggle.rectTransform.offsetMin = new Vector2(36f, 0f);
        textoToggle.rectTransform.offsetMax = Vector2.zero;
        LayoutElement toggleLayout = Layout(toggle.gameObject);
        toggleLayout.preferredWidth = 360f;
        toggleLayout.preferredHeight = 28f;
        Transform info = Buscar(filaFluctuaciones, "BotonInfo");
        BotonInfoCircular(info);
        Ordenar(filaFluctuaciones, toggle, info);

        Transform aviso = CrearAvisoError(formulario, "TextoError", canvas.GetComponentInChildren<ConfiguracionScreen>(true));

        Transform filaBotones = Buscar(canvas, "FilaBotones");
        filaBotones.SetParent(formulario, false);
        HorizontalLayoutGroup botones = Horizontal(filaBotones.gameObject, 0, 0, 12f, TextAnchor.MiddleCenter);
        botones.childControlWidth = true;
        botones.childForceExpandWidth = true;
        Layout(filaBotones.gameObject).preferredHeight = AltoBoton;
        Boton(Buscar(filaBotones, "BotonVolver"), Tema.EstiloBoton.Secundario, "Volver", AltoBoton);
        Boton(Buscar(filaBotones, "BotonGuardar"), Tema.EstiloBoton.Primario, "Guardar y continuar  →", AltoBoton);

        Ordenar(formulario, titulo, bajada, Espacio(formulario, "Espacio", 2f), gPisos, gFrecuencia, gPermanencia,
            filaHorario.transform, Espacio(formulario, "Espacio2", 2f), filaFluctuaciones, aviso, Espacio(formulario, "Espacio3", 4f), filaBotones);
    }

    // =====================================================================
    // Editor: barra superior, catálogo y ayuda a la izquierda, grilla al
    // centro, validación (o reproducción) a la derecha.
    // =====================================================================

    private const float MargenEditor = 24f;
    private const float AnchoColumnaIzquierda = 320f;
    private const float AnchoColumnaDerecha = 380f;
    private const float TamanioCeldaEditor = 56f;

    private static void DisenarEditor(Transform canvas)
    {
        Buscar(canvas, "Fondo").GetComponent<Image>().color = Tema.FondoEditor;
        Transform barra = BarraSuperior(canvas, "Editor de diseño", "Diseñá la planta del estacionamiento piso por piso");
        Transform acciones = Buscar(barra, "Acciones");

        // "Salir" queda visible también durante la reproducción (decisión
        // de Luca, bitácora 2026-10-09): va a la izquierda de la barra,
        // antes del título, fuera del grupo que se oculta.
        Transform salir = Buscar(canvas, "BotonSalir");
        salir.SetParent(barra, false);
        Boton(salir, Tema.EstiloBoton.Fantasma, "←  Inicio", 42f, 112f);
        Anclar(Rect(salir), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(184f, 0f), new Vector2(112f, 42f), new Vector2(0f, 0.5f));
        Rect(Buscar(barra, "Textos")).anchoredPosition = new Vector2(316f, 0f);
        GameObject divisorSalir = Hijo(barra, "DivisorSalir", typeof(Image));
        divisorSalir.GetComponent<Image>().color = Tema.Borde;
        Anclar(Rect(divisorSalir), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(302f, 0f), new Vector2(1f, 36f), new Vector2(0f, 0.5f));

        // Botones de edición: un grupo propio dentro de la barra, que es lo
        // que se oculta al pasar a la reproducción.
        GameObject accionesEdicion = Hijo(acciones, "AccionesEdicion");
        HorizontalLayoutGroup filaEdicion = Horizontal(accionesEdicion, 0, 0, 10f, TextAnchor.MiddleRight);
        filaEdicion.childControlWidth = true;
        Transform configuracion = Buscar(canvas, "BotonConfiguracion");
        Transform guardar = Buscar(canvas, "BotonGuardar");
        Transform simular = Buscar(canvas, "BotonSimular");
        foreach (Transform boton in new[] { configuracion, guardar, simular })
        {
            boton.SetParent(accionesEdicion.transform, false);
        }
        Boton(configuracion, Tema.EstiloBoton.Secundario, "Configuración", 46f, 170f);
        Boton(guardar, Tema.EstiloBoton.Secundario, "Guardar", 46f, 120f);
        Boton(simular, Tema.EstiloBoton.Primario, "▶  Simular", 46f, 150f);
        Ordenar(accionesEdicion.transform, configuracion, guardar, simular);
        LayoutElement accionesLayout = Layout(accionesEdicion);
        accionesLayout.preferredWidth = 170f + 120f + 150f + 20f;
        accionesLayout.preferredHeight = 46f;

        // --- Grilla ---
        Transform contenedor = Buscar(canvas, "ContenedorGrilla");
        GridLayoutGroup grilla = contenedor.GetComponent<GridLayoutGroup>();
        grilla.cellSize = new Vector2(TamanioCeldaEditor, TamanioCeldaEditor);
        grilla.spacing = new Vector2(2f, 2f);
        float centroX = (MargenEditor + AnchoColumnaIzquierda - MargenEditor - AnchoColumnaDerecha) / 2f;
        float ladoGrilla = 15 * (TamanioCeldaEditor + 2f) - 2f;
        float centroY = -(AltoBarraSuperior + 72f + ladoGrilla / 2f) + ResolucionReferencia.y / 2f;
        Rect(contenedor).anchoredPosition = new Vector2(centroX, centroY);

        // Marco detrás de la grilla: el espacio entre celdas deja ver su
        // color y funciona como líneas de la cuadrícula.
        GameObject marco = Hijo(canvas, "MarcoGrilla", typeof(Image), typeof(Shadow));
        marco.transform.SetSiblingIndex(contenedor.GetSiblingIndex());
        Anclar(Rect(marco), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(centroX, centroY), new Vector2(ladoGrilla + 20f, ladoGrilla + 20f), new Vector2(0.5f, 0.5f));
        Image imagenMarco = marco.GetComponent<Image>();
        imagenMarco.sprite = Tema.Redondeado(16);
        imagenMarco.type = Image.Type.Sliced;
        imagenMarco.color = Tema.Hex("#D5DCEA");
        imagenMarco.raycastTarget = false;
        Sombra(marco, 0.08f, 6f);

        // --- Pestañas de pisos y eliminar piso ---
        Transform selector = Buscar(canvas, "SelectorPisos");
        Anclar(Rect(selector), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(centroX - ladoGrilla / 2f - 10f, -(AltoBarraSuperior + 16f)), new Vector2(0f, 46f), new Vector2(0f, 1f));
        Image fondoSelector = selector.GetComponent<Image>();
        fondoSelector.sprite = Tema.RedondeadoConBorde(12);
        fondoSelector.type = Image.Type.Sliced;
        fondoSelector.color = Color.white;
        HorizontalLayoutGroup pestanas = selector.GetComponent<HorizontalLayoutGroup>();
        pestanas.padding = new RectOffset(5, 5, 5, 5);
        pestanas.spacing = 4f;
        pestanas.childAlignment = TextAnchor.MiddleLeft;
        pestanas.childControlWidth = true;
        pestanas.childControlHeight = true;
        pestanas.childForceExpandWidth = false;
        pestanas.childForceExpandHeight = false;
        ContentSizeFitter ajusteSelector = Componente<ContentSizeFitter>(selector.gameObject);
        ajusteSelector.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajusteSelector.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        Transform eliminarPiso = Buscar(canvas, "BotonEliminarPiso");
        Boton(eliminarPiso, Tema.EstiloBoton.PeligroSuave, "Eliminar este piso", 40f, 190f);
        Anclar(Rect(eliminarPiso), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(centroX + ladoGrilla / 2f + 10f, -(AltoBarraSuperior + 19f)), new Vector2(190f, 40f), new Vector2(1f, 1f));

        // --- Columna izquierda: catálogo y ayuda ---
        GameObject columnaIzquierda = Hijo(canvas, "ColumnaIzquierda");
        Anclar(Rect(columnaIzquierda), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(MargenEditor, -(AltoBarraSuperior + MargenEditor)), new Vector2(AnchoColumnaIzquierda, 0f), new Vector2(0f, 1f));
        Vertical(columnaIzquierda, 0, 0, 16f, TextAnchor.UpperLeft);
        AjustarAlto(columnaIzquierda.transform, -1f);
        Transform catalogo = DisenarCatalogo(canvas, columnaIzquierda.transform);
        Transform ayuda = TarjetaAyudaEditor(columnaIzquierda.transform);
        Ordenar(columnaIzquierda.transform, catalogo, ayuda);

        // --- Columna derecha: validación ---
        Transform validacion = TarjetaValidacion(canvas);

        // --- Aviso flotante (errores de colocación, "Proyecto guardado.") ---
        AvisoFlotanteEditor(canvas, centroX);

        // --- Reproducción ---
        DisenarReproduccion(canvas, acciones);

        DisenarModalCambiosSinGuardar(canvas);

        // Lo que se oculta al pasar a la reproducción: el catálogo con la
        // ayuda, Configuración/Guardar/Simular y la validación.
        ModoReproduccion modo = Object.FindFirstObjectByType<ModoReproduccion>(FindObjectsInactive.Include);
        SerializedObject so = new SerializedObject(modo);
        SerializedProperty ocultos = so.FindProperty("objetosDeEdicion");
        GameObject[] objetos = { columnaIzquierda, accionesEdicion, validacion.gameObject };
        ocultos.arraySize = objetos.Length;
        for (int i = 0; i < objetos.Length; i++)
        {
            ocultos.GetArrayElementAtIndex(i).objectReferenceValue = objetos[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // Del panel viejo del catálogo solo quedaba el título.
        Object.DestroyImmediate(Buscar(canvas, "PanelCatalogo").gameObject);
    }

    // "¿Guardar los cambios?" al salir del Editor con cambios sin guardar.
    private static void DisenarModalCambiosSinGuardar(Transform canvas)
    {
        GameObject panel = Hijo(canvas, "PanelCambiosSinGuardar", typeof(Image));
        Transform tarjeta = PrepararModal(panel.transform, 520f);

        // Canvas propio por encima de las piezas (sortingOrder 1 a 3, ver
        // MenuContextual) y del aviso flotante (6); sin GraphicRaycaster no
        // recibiría clics.
        Canvas canvasModal = Componente<Canvas>(panel);
        canvasModal.overrideSorting = true;
        canvasModal.sortingOrder = 9;
        Componente<GraphicRaycaster>(panel);

        Transform icono = IconoAlerta(tarjeta, "Icono", "!", Tema.AmbarSuave, Tema.Hex("#C77700"));
        Transform titulo = Titulo(tarjeta, "Titulo", "¿Guardar los cambios?", 28f);
        Transform bajada = Parrafo(tarjeta, "Bajada", "El diseño tiene cambios que todavía no guardaste. Si salís sin guardar, se pierden.");

        Transform guardar = NuevoBoton(tarjeta, "BotonGuardarYSalir");
        Boton(guardar, Tema.EstiloBoton.Primario, "Guardar y salir", AltoBoton);
        Transform cancelar = NuevoBoton(tarjeta, "BotonCancelarSalida");
        Transform salirSinGuardar = NuevoBoton(tarjeta, "BotonSalirSinGuardar");
        Transform fila = FilaBotones(tarjeta, cancelar, salirSinGuardar);
        Boton(cancelar, Tema.EstiloBoton.Secundario, "Cancelar", AltoBoton);
        Boton(salirSinGuardar, Tema.EstiloBoton.PeligroSuave, "Salir sin guardar", AltoBoton);
        Ordenar(tarjeta, icono, titulo, bajada, Espacio(tarjeta, "Espacio", 6f), guardar, fila);

        EditorScreen pantalla = Object.FindFirstObjectByType<EditorScreen>(FindObjectsInactive.Include);
        Asignar(pantalla, "panelCambiosSinGuardar", panel);
        Asignar(pantalla, "botonGuardarYSalir", guardar.GetComponent<Button>());
        Asignar(pantalla, "botonSalirSinGuardar", salirSinGuardar.GetComponent<Button>());
        Asignar(pantalla, "botonCancelarSalida", cancelar.GetComponent<Button>());
        panel.SetActive(false);
    }

    private struct PiezaCatalogo
    {
        public string Objeto;
        public string Etiqueta;
        public string Nombre;
        public string Descripcion;
    }

    // Orden del catálogo: primero lo que todo diseño necesita.
    private static readonly PiezaCatalogo[] Piezas =
    {
        new PiezaCatalogo { Objeto = "CatalogoItemEntrada", Etiqueta = "Entrada", Nombre = "Entrada",
            Descripcion = "Por donde ingresan los vehículos. Va en el borde de la grilla, solo en planta baja, y hay una por proyecto. La flecha apunta hacia adentro." },
        new PiezaCatalogo { Objeto = "CatalogoItemSalida", Etiqueta = "Salida", Nombre = "Salida",
            Descripcion = "Por donde salen los vehículos. Va en el borde de la grilla, solo en planta baja, y hay una por proyecto. La flecha apunta hacia afuera." },
        new PiezaCatalogo { Objeto = "CatalogoItemCalle", Etiqueta = "Calle", Nombre = "Calle",
            Descripcion = "Circulación de los vehículos; la flecha marca el sentido. Las curvas y los cruces se dibujan solos. Clic derecho: cruce peatonal." },
        new PiezaCatalogo { Objeto = "CatalogoItemPlaza", Etiqueta = "Plaza", Nombre = "Plaza",
            Descripcion = "Lugar para un auto. Ocupa dos celdas y su boca tiene que dar a una calle. Clic derecho: marcarla como plaza accesible." },
        new PiezaCatalogo { Objeto = "CatalogoItemZonaBiciMoto", Etiqueta = "Bicis y motos", Nombre = "Zona de bicicletas y motos",
            Descripcion = "Cinco lugares para bicicletas o motos en una celda. Su boca tiene que dar a una calle." },
        new PiezaCatalogo { Objeto = "CatalogoItemEscalera", Etiqueta = "Escalera", Nombre = "Escalera",
            Descripcion = "Salida peatonal y de emergencia. Se coloca una vez y ocupa la misma celda en todos los pisos." },
        new PiezaCatalogo { Objeto = "CatalogoItemRampaSube", Etiqueta = "Rampa sube", Nombre = "Rampa que sube",
            Descripcion = "Conecta este piso con el de arriba. Se coloca una vez y el sistema crea su celda en los dos pisos." },
        new PiezaCatalogo { Objeto = "CatalogoItemRampaBaja", Etiqueta = "Rampa baja", Nombre = "Rampa que baja",
            Descripcion = "Conecta este piso con el de abajo. Se coloca una vez y el sistema crea su celda en los dos pisos." },
    };

    private static Transform DisenarCatalogo(Transform canvas, Transform columna)
    {
        Transform tarjeta = Tarjeta(columna, "TarjetaCatalogo", 20, 20, 14f);
        Transform titulo = Subtitulo(tarjeta, "Titulo", "Piezas");
        Transform bajada = Parrafo(tarjeta, "Bajada", "Arrastrá una pieza hasta la grilla.", 14f);

        GameObject cuadricula = Hijo(tarjeta, "Cuadricula", typeof(GridLayoutGroup));
        GridLayoutGroup grid = cuadricula.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(134f, 92f);
        grid.spacing = new Vector2(12f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        Layout(cuadricula).preferredHeight = 4 * 92f + 3 * 10f;

        // Recuadro de detalle: qué es la pieza que está bajo el puntero.
        GameObject detalle = Hijo(tarjeta, "Detalle", typeof(Image));
        Image fondoDetalle = detalle.GetComponent<Image>();
        fondoDetalle.sprite = Tema.Redondeado(12);
        fondoDetalle.type = Image.Type.Sliced;
        fondoDetalle.color = Tema.PrimarioSuave;
        Vertical(detalle, 16, 12, 4f, TextAnchor.UpperLeft);
        LayoutElement detalleLayout = Layout(detalle);
        detalleLayout.preferredHeight = 112f;
        detalleLayout.minHeight = 112f;
        TMP_Text tituloDetalle = TextoEn(detalle.transform, "TituloDetalle", AyudaPiezaCatalogo.TituloPorDefecto, Tema.FuenteSubtitulo, 15f, Tema.PrimarioOscuro, TextAlignmentOptions.TopLeft);
        TMP_Text textoDetalle = TextoEn(detalle.transform, "TextoDetalle", AyudaPiezaCatalogo.DescripcionPorDefecto, Tema.FuenteTexto, 13.5f, Tema.TextoSecundario, TextAlignmentOptions.TopLeft);
        textoDetalle.lineSpacing = 4f;
        Ordenar(detalle.transform, tituloDetalle.transform, textoDetalle.transform);

        foreach (PiezaCatalogo pieza in Piezas)
        {
            Transform item = Buscar(canvas, pieza.Objeto);
            item.SetParent(cuadricula.transform, false);
            Image imagen = item.GetComponent<Image>();
            Sprite spritePieza = imagen.sprite;
            if (spritePieza != null && spritePieza.name.StartsWith("Borde"))
            {
                spritePieza = Buscar(item, "Icono").GetComponent<Image>().sprite;
            }
            imagen.sprite = Tema.RedondeadoConBorde(12);
            imagen.type = Image.Type.Sliced;
            imagen.color = Tema.Superficie;
            imagen.preserveAspect = false;
            imagen.raycastTarget = true;

            Image icono = ImagenEn(item, "Icono", spritePieza);
            icono.preserveAspect = true;
            Anclar(icono.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(80f, 50f), new Vector2(0.5f, 1f));
            TMP_Text nombre = TextoEn(item, "Nombre", pieza.Etiqueta, Tema.FuenteTextoFuerte, 14f, Tema.Texto, TextAlignmentOptions.Center);
            Anclar(nombre.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(-12f, 22f), new Vector2(0.5f, 0f));

            AyudaPiezaCatalogo ayuda = Componente<AyudaPiezaCatalogo>(item.gameObject);
            Asignar(ayuda, "nombrePieza", pieza.Nombre);
            Asignar(ayuda, "descripcion", pieza.Descripcion);
            Asignar(ayuda, "textoTituloDetalle", tituloDetalle);
            Asignar(ayuda, "textoDescripcionDetalle", textoDetalle);
        }

        Ordenar(tarjeta, titulo, bajada, cuadricula.transform, detalle.transform);
        return tarjeta;
    }

    private static Transform TarjetaAyudaEditor(Transform columna)
    {
        Transform tarjeta = Tarjeta(columna, "TarjetaAyuda", 20, 18, 10f);
        Transform titulo = Subtitulo(tarjeta, "Titulo", "Cómo se usa");
        var filas = new List<Transform> { titulo };
        (string tecla, string texto)[] atajos =
        {
            ("Arrastrar", "Llevá una pieza del catálogo a la grilla."),
            ("R", "Rotá la pieza mientras la arrastrás."),
            ("Clic der.", "Opciones: accesible, cruce peatonal, eliminar."),
            ("Mover", "Arrastrá una pieza colocada a otra celda, o afuera de la grilla para quitarla."),
        };
        for (int i = 0; i < atajos.Length; i++)
        {
            GameObject fila = Hijo(tarjeta, $"Atajo{i}");
            HorizontalLayoutGroup h = Horizontal(fila, 0, 0, 12f, TextAnchor.MiddleLeft);
            h.childControlWidth = true;
            Transform tecla = Pildora(fila.transform, "Tecla", atajos[i].tecla, Tema.SuperficieSuave, Tema.PrimarioOscuro, 13f, true);
            LayoutElement teclaLayout = tecla.GetComponent<LayoutElement>();
            teclaLayout.preferredWidth = teclaLayout.minWidth = 82f;
            teclaLayout.preferredHeight = 30f;
            TMP_Text texto = TextoEn(fila.transform, "Texto", atajos[i].texto, Tema.FuenteTexto, 13.5f, Tema.TextoSecundario, TextAlignmentOptions.MidlineLeft);
            Layout(texto.gameObject).flexibleWidth = 1f;
            Ordenar(fila.transform, tecla, texto.transform);
            filas.Add(fila.transform);
        }
        Ordenar(tarjeta, filas.ToArray());
        return tarjeta;
    }

    private static Transform TarjetaValidacion(Transform canvas)
    {
        GameObject tarjeta = Hijo(canvas, "TarjetaValidacion", typeof(Image), typeof(Shadow));
        EstiloTarjeta(tarjeta, 20);
        RectTransform rect = Rect(tarjeta);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(-MargenEditor - AnchoColumnaDerecha, MargenEditor);
        rect.offsetMax = new Vector2(-MargenEditor, -(AltoBarraSuperior + MargenEditor));
        Vertical(tarjeta, 24, 22, 14f, TextAnchor.UpperLeft);

        Transform titulo = Subtitulo(tarjeta.transform, "Titulo", "Validación del diseño");
        Transform bajada = Parrafo(tarjeta.transform, "Bajada", "Se revisa en cada cambio. Las celdas con problemas se marcan en rojo.", 14f);

        GameObject estado = Hijo(tarjeta.transform, "Estado", typeof(Image));
        Image fondoEstado = estado.GetComponent<Image>();
        fondoEstado.sprite = Tema.Redondeado(10);
        fondoEstado.type = Image.Type.Sliced;
        fondoEstado.color = Tema.PeligroSuave;
        LayoutElement estadoLayout = Layout(estado);
        estadoLayout.preferredHeight = 40f;
        TMP_Text textoEstado = TextoEn(estado.transform, "TextoEstado", "Validando…", Tema.FuenteTextoFuerte, 15f, Tema.PeligroOscuro, TextAlignmentOptions.MidlineLeft);
        Tema.Estirar(textoEstado.rectTransform, 14f, 0f);

        Transform advertencias = Buscar(canvas, "TextoAdvertencias");
        TMP_Text textoAdvertencias = advertencias.GetComponent<TMP_Text>();
        EstiloTexto(textoAdvertencias, Tema.FuenteTexto, 15f, Tema.Texto, TextAlignmentOptions.TopLeft);
        textoAdvertencias.textWrappingMode = TextWrappingModes.Normal;
        textoAdvertencias.paragraphSpacing = 14f;
        textoAdvertencias.lineSpacing = 2f;
        textoAdvertencias.text = string.Empty;
        Transform lista = ListaDesplazable(tarjeta.transform, "ListaAdvertencias", advertencias);
        Layout(lista.gameObject).flexibleHeight = 1f;

        PanelAdvertencias panel = advertencias.GetComponent<PanelAdvertencias>();
        Asignar(panel, "fondoEstado", fondoEstado);
        Asignar(panel, "textoEstado", textoEstado);

        Ordenar(tarjeta.transform, titulo, bajada, estado.transform, lista);
        return tarjeta.transform;
    }

    private static void AvisoFlotanteEditor(Transform canvas, float centroX)
    {
        Transform textoMensaje = Buscar(canvas, "TextoMensajeEditor");
        GameObject aviso = Hijo(canvas, "AvisoEditor", typeof(Image), typeof(Shadow), typeof(Canvas));
        aviso.transform.SetAsLastSibling();
        Anclar(Rect(aviso), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(centroX, 36f), new Vector2(0f, 56f), new Vector2(0.5f, 0f));
        Image fondo = aviso.GetComponent<Image>();
        fondo.sprite = Tema.Redondeado(16);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.Peligro;
        fondo.raycastTarget = false;
        Sombra(aviso, 0.25f, 8f);

        // Encima de las piezas (sortingOrder 1) y las marcas rojas (2).
        Canvas canvasAviso = aviso.GetComponent<Canvas>();
        canvasAviso.overrideSorting = true;
        canvasAviso.sortingOrder = 6;

        HorizontalLayoutGroup fila = Horizontal(aviso, 18, 12, 12f, TextAnchor.MiddleLeft);
        fila.childControlWidth = true;
        ContentSizeFitter ajuste = Componente<ContentSizeFitter>(aviso);
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject circulo = Hijo(aviso.transform, "Icono", typeof(Image));
        Image imagenCirculo = circulo.GetComponent<Image>();
        imagenCirculo.sprite = Tema.Circulo;
        imagenCirculo.color = new Color(1f, 1f, 1f, 0.25f);
        imagenCirculo.raycastTarget = false;
        LayoutElement circuloLayout = Layout(circulo);
        circuloLayout.preferredWidth = circuloLayout.preferredHeight = 30f;
        TMP_Text icono = TextoEn(circulo.transform, "Simbolo", "!", Tema.FuenteTitulo, 17f, Color.white, TextAlignmentOptions.Center);
        Tema.Estirar(icono.rectTransform);

        textoMensaje.SetParent(aviso.transform, false);
        TMP_Text texto = textoMensaje.GetComponent<TMP_Text>();
        EstiloTexto(texto, Tema.FuenteTextoFuerte, 17f, Color.white, TextAlignmentOptions.MidlineLeft);
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.text = "La celda está ocupada.";
        textoMensaje.gameObject.SetActive(true);
        Ordenar(aviso.transform, circulo.transform, textoMensaje);
        aviso.SetActive(false);

        EditorScreen pantalla = Object.FindFirstObjectByType<EditorScreen>(FindObjectsInactive.Include);
        Asignar(pantalla, "fondoMensaje", fondo);
        Asignar(pantalla, "iconoMensaje", icono);
        Asignar(pantalla, "textoMensaje", texto);
    }

    private static void DisenarReproduccion(Transform canvas, Transform accionesBarra)
    {
        Transform panel = Buscar(canvas, "PanelReproduccion");
        ModoReproduccion modo = Object.FindFirstObjectByType<ModoReproduccion>(FindObjectsInactive.Include);

        // "Ver resultados" va en la barra superior, donde estaba "Simular".
        Transform verResultados = Buscar(panel, "BotonVerResultados");
        Boton(verResultados, Tema.EstiloBoton.Primario, "Ver resultados  →", 46f, 210f);
        Anclar(Rect(verResultados), Vector2.one, Vector2.one, new Vector2(-28f, -(AltoBarraSuperior - 46f) / 2f), new Vector2(210f, 46f), Vector2.one);
        verResultados.GetComponentInChildren<TMP_Text>().text = "Ver resultados  →";

        GameObject tarjeta = Hijo(panel, "TarjetaReproduccion", typeof(Image), typeof(Shadow));
        EstiloTarjeta(tarjeta, 20);
        Anclar(Rect(tarjeta), Vector2.one, Vector2.one, new Vector2(-MargenEditor, -(AltoBarraSuperior + MargenEditor)), new Vector2(AnchoColumnaDerecha, 0f), Vector2.one);
        Vertical(tarjeta, 24, 22, 14f, TextAnchor.UpperLeft);
        AjustarAlto(tarjeta.transform, -1f);

        Transform titulo = Subtitulo(tarjeta.transform, "Titulo", "Reproducción de la simulación");
        Transform etiquetaHora = Etiqueta(tarjeta.transform, "EtiquetaHora", "HORA SIMULADA", Tema.TextoTenue, 12f);
        Transform hora = Buscar(panel, "TextoHora");
        EstiloTexto(hora.GetComponent<TMP_Text>(), Tema.FuenteTitulo, 56f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        Layout(hora.gameObject).preferredHeight = 64f;

        // Contadores en dos recuadros.
        GameObject contadores = Hijo(tarjeta.transform, "Contadores");
        HorizontalLayoutGroup filaContadores = Horizontal(contadores, 0, 0, 10f, TextAnchor.MiddleLeft);
        filaContadores.childControlWidth = true;
        filaContadores.childForceExpandWidth = true;
        Layout(contadores).preferredHeight = 52f;
        Transform ocupadas = Recuadro(contadores.transform, "RecuadroOcupadas", Buscar(panel, "TextoOcupadas"), Tema.PrimarioSuave, Tema.PrimarioOscuro);
        Transform rechazados = Recuadro(contadores.transform, "RecuadroRechazados", Buscar(panel, "TextoRechazados"), Tema.AmbarSuave, Tema.Hex("#A15C00"));
        Ordenar(contadores.transform, ocupadas, rechazados);

        // "Saturado": una píldora roja que se prende y se apaga entera.
        Transform saturado = Buscar(panel, "TextoSaturado");
        GameObject pildora = Hijo(tarjeta.transform, "AvisoSaturado", typeof(Image));
        Image fondoPildora = pildora.GetComponent<Image>();
        fondoPildora.sprite = Tema.Redondeado(10);
        fondoPildora.type = Image.Type.Sliced;
        fondoPildora.color = Tema.Peligro;
        Layout(pildora).preferredHeight = 40f;
        saturado.SetParent(pildora.transform, false);
        Tema.Estirar(Rect(saturado), 14f, 0f);
        EstiloTexto(saturado.GetComponent<TMP_Text>(), Tema.FuenteTextoFuerte, 15f, Color.white, TextAlignmentOptions.MidlineLeft);
        saturado.GetComponent<TMP_Text>().text = "●  Estacionamiento saturado";
        saturado.gameObject.SetActive(true);
        Asignar(modo, "avisoSaturado", pildora);

        Transform etiquetaControles = Etiqueta(tarjeta.transform, "EtiquetaControles", "CONTROLES", Tema.TextoTenue, 12f);
        Transform playPausa = Buscar(panel, "BotonVerPlayPusa");
        Boton(playPausa, Tema.EstiloBoton.Primario, "▶  Reproducir", AltoBoton);

        GameObject velocidades = Hijo(tarjeta.transform, "Velocidades");
        HorizontalLayoutGroup filaVelocidades = Horizontal(velocidades, 0, 0, 8f, TextAnchor.MiddleLeft);
        filaVelocidades.childControlWidth = true;
        filaVelocidades.childForceExpandWidth = true;
        Layout(velocidades).preferredHeight = 44f;
        string[] botonesVelocidad = { "Botonx1", "Botonx5", "Botonx20" };
        string[] textosVelocidad = { "x1", "x5", "x20" };
        for (int i = 0; i < botonesVelocidad.Length; i++)
        {
            Transform boton = Buscar(panel, botonesVelocidad[i]);
            boton.SetParent(velocidades.transform, false);
            Boton(boton, Tema.EstiloBoton.Secundario, textosVelocidad[i], 44f);
            // La elegida queda deshabilitada (ModoReproduccion.MarcarVelocidad):
            // tiene que verse rellena, no apagada.
            Button componente = boton.GetComponent<Button>();
            ColorBlock colores = componente.colors;
            colores.disabledColor = Color.white;
            componente.colors = colores;
        }

        Transform leyenda = Buscar(panel, "TextoLeyenda");
        EstiloTexto(leyenda.GetComponent<TMP_Text>(), Tema.FuenteTexto, 13.5f, Tema.TextoTenue, TextAlignmentOptions.TopLeft);
        leyenda.GetComponent<TMP_Text>().fontStyle = FontStyles.Italic;
        leyenda.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
        leyenda.GetComponent<TMP_Text>().text = "La circulación es ilustrativa; los indicadores no contemplan tiempos de recorrido ni congestión.";

        Ordenar(tarjeta.transform, titulo, etiquetaHora, hora, contadores.transform, pildora.transform,
            Espacio(tarjeta.transform, "Espacio", 2f), etiquetaControles, playPausa, velocidades.transform, leyenda);
        Asignar(modo, "textoPlayPausa", playPausa.GetComponentInChildren<TMP_Text>());
    }

    // Recuadro de color suave con un texto adentro (contadores de la reproducción).
    private static Transform Recuadro(Transform padre, string nombre, Transform texto, Color fondo, Color colorTexto)
    {
        GameObject recuadro = Hijo(padre, nombre, typeof(Image));
        Image imagen = recuadro.GetComponent<Image>();
        imagen.sprite = Tema.Redondeado(10);
        imagen.type = Image.Type.Sliced;
        imagen.color = fondo;
        Layout(recuadro).preferredHeight = 52f;
        texto.SetParent(recuadro.transform, false);
        Tema.Estirar(Rect(texto), 12f, 0f);
        EstiloTexto(texto.GetComponent<TMP_Text>(), Tema.FuenteTextoFuerte, 15f, colorTexto, TextAlignmentOptions.Center);
        return recuadro.transform;
    }

    // =====================================================================
    // Resultados: barra superior, cuatro tarjetas de indicadores y la curva.
    // =====================================================================

    private static void DisenarResultados(Transform canvas)
    {
        Transform barra = BarraSuperior(canvas, "Resultados de la simulación", "Indicadores y curva de ocupación");
        Transform acciones = Buscar(barra, "Acciones");
        Transform panel = Buscar(canvas, "PanelResultados");
        ResultadosScreen pantalla = canvas.GetComponentInChildren<ResultadosScreen>(true);

        Transform volver = Buscar(panel, "BotonVolver");
        Transform guardar = Buscar(panel, "BotonGuardarResultados");
        volver.SetParent(acciones, false);
        guardar.SetParent(acciones, false);
        Boton(volver, Tema.EstiloBoton.Secundario, "←  Volver", 46f, 130f);
        Boton(guardar, Tema.EstiloBoton.Primario, "Guardar resultados", 46f, 210f);
        Ordenar(acciones, volver, guardar);

        RectTransform panelRect = Rect(panel);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = new Vector2(0f, -AltoBarraSuperior);
        panel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        VerticalLayoutGroup columna = Vertical(panel.gameObject, 64, 40, 24f, TextAnchor.UpperLeft);
        columna.padding.bottom = 40;

        GameObject encabezado = Hijo(panel, "Encabezado");
        Vertical(encabezado, 0, 0, 4f, TextAnchor.UpperLeft);
        Transform titulo = Titulo(encabezado.transform, "Titulo", "Cómo funcionó tu diseño", 36f);
        Transform estadoGuardado = Buscar(panel, "TextoEstadoGuardado");
        estadoGuardado.SetParent(encabezado.transform, false);
        EstiloTexto(estadoGuardado.GetComponent<TMP_Text>(), Tema.FuenteTexto, 17f, Tema.TextoSecundario, TextAlignmentOptions.TopLeft);
        estadoGuardado.GetComponent<TMP_Text>().text = "Estos resultados todavía no están guardados.";
        Ordenar(encabezado.transform, titulo, estadoGuardado);

        // Cuatro tarjetas de indicadores.
        Transform indicadores = Buscar(panel, "Indicadores");
        Object.DestroyImmediate(indicadores.GetComponent<ContentSizeFitter>());
        Object.DestroyImmediate(indicadores.GetComponent<VerticalLayoutGroup>());
        HorizontalLayoutGroup filaIndicadores = Horizontal(indicadores.gameObject, 0, 0, 20f, TextAnchor.UpperLeft);
        filaIndicadores.childControlWidth = true;
        filaIndicadores.childForceExpandWidth = true;
        Layout(indicadores.gameObject).preferredHeight = 186f;

        (string texto, string etiqueta, Color acento, string campoDetalle)[] tarjetas =
        {
            ("TextoEficiencia", "EFICIENCIA ESPACIAL", Tema.Primario, "textoEficienciaDetalle"),
            ("TextoDemanda", "DEMANDA SATISFECHA", Tema.Exito, "textoDemandaDetalle"),
            ("TextoSaturacion", "TIEMPO DE SATURACIÓN", Tema.Ambar, "textoSaturacionDetalle"),
            ("TextoPuntuacion", "PUNTUACIÓN GENERAL", Tema.Acento, "textoPuntuacionDetalle"),
        };
        var tarjetasCreadas = new List<Transform>();
        foreach (var definicion in tarjetas)
        {
            Transform tarjeta = Tarjeta(indicadores, "Tarjeta" + definicion.texto.Replace("Texto", ""), 24, 22, 6f);
            // Las cuatro del mismo ancho, sin importar lo largo de su texto:
            // el ancho preferido fijo evita que una tarjeta con un detalle
            // largo se ensanche y corra a las otras.
            LayoutElement tarjetaLayout = Layout(tarjeta.gameObject);
            tarjetaLayout.preferredWidth = tarjetaLayout.minWidth = 10f;
            tarjetaLayout.flexibleWidth = 1f;
            GameObject cabecera = Hijo(tarjeta, "Cabecera");
            HorizontalLayoutGroup filaCabecera = Horizontal(cabecera, 0, 0, 8f, TextAnchor.MiddleLeft);
            filaCabecera.childControlWidth = true;
            Layout(cabecera).preferredHeight = 20f;
            GameObject punto = Hijo(cabecera.transform, "Punto", typeof(Image));
            punto.GetComponent<Image>().sprite = Tema.Circulo;
            punto.GetComponent<Image>().color = definicion.acento;
            LayoutElement puntoLayout = Layout(punto);
            puntoLayout.preferredWidth = puntoLayout.minWidth = 10f;
            puntoLayout.preferredHeight = 10f;
            Transform etiqueta = Etiqueta(cabecera.transform, "Etiqueta", definicion.etiqueta, Tema.TextoSecundario, 12.5f);
            Layout(etiqueta.gameObject).flexibleWidth = 1f;
            Ordenar(cabecera.transform, punto.transform, etiqueta);

            Transform valor = Buscar(panel, "Indicadores/" + definicion.texto);
            valor.SetParent(tarjeta, false);
            TMP_Text textoValor = valor.GetComponent<TMP_Text>();
            EstiloTexto(textoValor, Tema.FuenteTitulo, 40f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
            textoValor.textWrappingMode = TextWrappingModes.NoWrap;
            textoValor.overflowMode = TextOverflowModes.Overflow;
            textoValor.text = "—";
            LayoutElement valorLayout = Layout(valor.gameObject);
            valorLayout.preferredHeight = valorLayout.minHeight = 62f;

            TMP_Text detalle = TextoEn(tarjeta, "Detalle", string.Empty, Tema.FuenteTexto, 15f, Tema.TextoSecundario, TextAlignmentOptions.TopLeft);
            detalle.textWrappingMode = TextWrappingModes.Normal;
            detalle.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement detalleLayout = Layout(detalle.gameObject);
            detalleLayout.preferredHeight = detalleLayout.minHeight = 40f;
            Asignar(pantalla, definicion.campoDetalle, detalle);

            Ordenar(tarjeta, cabecera.transform, valor, detalle.transform);
            tarjetasCreadas.Add(tarjeta);
        }
        Ordenar(indicadores, tarjetasCreadas.ToArray());

        // Tarjeta del gráfico.
        Transform tarjetaGrafico = Tarjeta(panel, "TarjetaGrafico", 28, 24, 10f);
        Layout(tarjetaGrafico.gameObject).flexibleHeight = 1f;
        Transform tituloGrafico = Subtitulo(tarjetaGrafico, "Titulo", "Curva de ocupación");
        Transform bajadaGrafico = Parrafo(tarjetaGrafico, "Bajada", "Plazas ocupadas a lo largo del horario simulado. El techo del gráfico es el total de plazas.", 15f);
        Transform fondoGrafico = Buscar(panel, "FondoGrafico");
        fondoGrafico.SetParent(tarjetaGrafico, false);
        fondoGrafico.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        Layout(fondoGrafico.gameObject).flexibleHeight = 1f;
        RectTransform graficoRect = Rect(Buscar(fondoGrafico, "GraficoCurva"));
        graficoRect.anchorMin = Vector2.zero;
        graficoRect.anchorMax = Vector2.one;
        graficoRect.pivot = new Vector2(0.5f, 0.5f);
        graficoRect.offsetMin = new Vector2(56f, 40f);
        graficoRect.offsetMax = new Vector2(-12f, -14f);
        GraficoCurva grafico = graficoRect.GetComponent<GraficoCurva>();
        grafico.color = Tema.Primario;
        Asignar(grafico, "colorEjes", Tema.Hex("#C5CDDD"));
        Asignar(grafico, "colorGrilla", Tema.Hex("#EDF0F6"));
        Asignar(grafico, "colorEtiquetas", Tema.TextoSecundario);
        Asignar(grafico, "grosorLinea", 3.5f);
        Asignar(grafico, "tamanoEtiquetas", 14f);
        TMP_Text periodos = TextoEn(tarjetaGrafico, "TextoPeriodosSaturacion", string.Empty,
            Tema.FuenteTextoFuerte, 15f, Tema.Hex("#A15C00"), TextAlignmentOptions.TopLeft);
        periodos.textWrappingMode = TextWrappingModes.Normal;
        periodos.lineSpacing = 4f;
        Asignar(pantalla, "textoPeriodosSaturacion", periodos);
        Ordenar(tarjetaGrafico, tituloGrafico, bajadaGrafico, fondoGrafico, periodos.transform);

        Ordenar(panel, encabezado.transform, indicadores, tarjetaGrafico);
    }

    // =====================================================================
    // Prefabs: fila de proyecto (Inicio) y celda de la grilla (Editor).
    // =====================================================================

    private static void DisenarPrefabs()
    {
        const string rutaItem = "Assets/Prefabs/ItemProyecto.prefab";
        GameObject item = PrefabUtility.LoadPrefabContents(rutaItem);
        Image fondo = Componente<Image>(item);
        fondo.sprite = Tema.RedondeadoConBorde(12);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.Superficie;
        Rect(item).sizeDelta = new Vector2(600f, 60f);
        LayoutElement layout = Layout(item);
        layout.preferredHeight = layout.minHeight = 60f;

        Toggle toggle = item.GetComponent<Toggle>();
        Casilla(toggle);
        // La fila entera responde al puntero, no solo la casilla.
        toggle.targetGraphic = fondo;
        ColorBlock colores = toggle.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(0.95f, 0.95f, 0.99f, 1f);
        colores.pressedColor = new Color(0.9f, 0.9f, 0.97f, 1f);
        colores.selectedColor = Color.white;
        toggle.colors = colores;
        toggle.transition = Selectable.Transition.ColorTint;
        RectTransform casilla = Rect(item.transform.Find("Background"));
        Anclar(casilla, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(24f, 24f), new Vector2(0f, 0.5f));

        TMP_Text nombre = item.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
        EstiloTexto(nombre, Tema.FuenteTextoFuerte, 17f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        nombre.textWrappingMode = TextWrappingModes.NoWrap;
        nombre.overflowMode = TextOverflowModes.Ellipsis;
        nombre.rectTransform.anchorMin = Vector2.zero;
        nombre.rectTransform.anchorMax = Vector2.one;
        nombre.rectTransform.offsetMin = new Vector2(62f, 0f);
        nombre.rectTransform.offsetMax = new Vector2(-20f, 0f);
        PrefabUtility.SaveAsPrefabAsset(item, rutaItem);
        PrefabUtility.UnloadPrefabContents(item);

        const string rutaCelda = "Assets/Prefabs/Celda.prefab";
        GameObject celda = PrefabUtility.LoadPrefabContents(rutaCelda);
        celda.GetComponent<Image>().color = Tema.Hex("#F8F9FD");
        PrefabUtility.SaveAsPrefabAsset(celda, rutaCelda);
        PrefabUtility.UnloadPrefabContents(celda);
    }

    // =====================================================================
    // Piezas reutilizables de diseño
    // =====================================================================

    // Barra blanca de arriba con la marca, el título de la pantalla y un
    // contenedor "Acciones" a la derecha para los botones.
    private static Transform BarraSuperior(Transform canvas, string titulo, string bajada)
    {
        GameObject barra = Hijo(canvas, "BarraSuperior", typeof(Image));
        barra.transform.SetSiblingIndex(1);
        RectTransform rect = Rect(barra);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, AltoBarraSuperior);
        rect.anchoredPosition = Vector2.zero;
        barra.GetComponent<Image>().color = Tema.Superficie;
        LineaBorde(barra.transform, "BordeInferior", true);

        Transform marca = Marca(barra.transform, false);
        Anclar(Rect(marca), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(150f, 44f), new Vector2(0f, 0.5f));

        GameObject divisor = Hijo(barra.transform, "Divisor", typeof(Image));
        divisor.GetComponent<Image>().color = Tema.Borde;
        Anclar(Rect(divisor), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 0f), new Vector2(1f, 36f), new Vector2(0f, 0.5f));

        GameObject textos = Hijo(barra.transform, "Textos");
        Anclar(Rect(textos), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(194f, 0f), new Vector2(700f, 52f), new Vector2(0f, 0.5f));
        Vertical(textos, 0, 0, 0f, TextAnchor.MiddleLeft);
        TMP_Text textoTitulo = TextoEn(textos.transform, "Titulo", titulo, Tema.FuenteSubtitulo, 18f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        TMP_Text textoBajada = TextoEn(textos.transform, "Bajada", bajada, Tema.FuenteTexto, 14f, Tema.TextoTenue, TextAlignmentOptions.MidlineLeft);
        Ordenar(textos.transform, textoTitulo.transform, textoBajada.transform);

        GameObject acciones = Hijo(barra.transform, "Acciones");
        Anclar(Rect(acciones), Vector2.one, Vector2.one, new Vector2(-28f, 0f), new Vector2(900f, AltoBarraSuperior), Vector2.one);
        HorizontalLayoutGroup fila = Horizontal(acciones, 0, 0, 10f, TextAnchor.MiddleRight);
        fila.childControlWidth = true;
        return barra.transform;
    }

    // Logo: cuadrado redondeado con el degradado de marca y una "P" de
    // estacionamiento, más el nombre del sistema.
    private static Transform Marca(Transform padre, bool sobreColor)
    {
        GameObject marca = Hijo(padre, "Marca");
        HorizontalLayoutGroup fila = Horizontal(marca, 0, 0, 12f, TextAnchor.MiddleLeft);
        fila.childControlWidth = true;

        GameObject logo = Hijo(marca.transform, "Logo", typeof(Image));
        Image imagenLogo = logo.GetComponent<Image>();
        imagenLogo.sprite = sobreColor ? Tema.Redondeado(12) : SpriteUI("DegradadoLogo");
        imagenLogo.type = sobreColor ? Image.Type.Sliced : Image.Type.Simple;
        imagenLogo.color = sobreColor ? Color.white : Color.white;
        imagenLogo.raycastTarget = false;
        LayoutElement logoLayout = Layout(logo);
        logoLayout.preferredWidth = logoLayout.minWidth = 44f;
        logoLayout.preferredHeight = 44f;
        TMP_Text letra = TextoEn(logo.transform, "Letra", "P", Tema.FuenteTitulo, 24f, sobreColor ? Tema.Primario : Color.white, TextAlignmentOptions.Center);
        Tema.Estirar(letra.rectTransform);
        letra.margin = new Vector4(0f, 3f, 0f, 0f);

        TMP_Text nombre = TextoEn(marca.transform, "Nombre", "SME", Tema.FuenteTitulo, 24f, sobreColor ? Color.white : Tema.Texto, TextAlignmentOptions.MidlineLeft);
        nombre.characterSpacing = 4f;
        Layout(nombre.gameObject).preferredWidth = 80f;
        Ordenar(marca.transform, logo.transform, nombre.transform);
        return marca.transform;
    }

    private static Transform Tarjeta(Transform padre, string nombre, int padH, int padV, float espacio)
    {
        GameObject tarjeta = Hijo(padre, nombre, typeof(Image), typeof(Shadow));
        EstiloTarjeta(tarjeta, 20);
        Vertical(tarjeta, padH, padV, espacio, TextAnchor.UpperLeft);
        return tarjeta.transform;
    }

    private static void EstiloTarjeta(GameObject tarjeta, int radio)
    {
        Image fondo = Componente<Image>(tarjeta);
        fondo.sprite = Tema.RedondeadoConBorde(radio);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.Superficie;
        Sombra(tarjeta, 0.06f, 4f);
    }

    private static void Sombra(GameObject objeto, float alfa, float distancia)
    {
        Shadow sombra = Componente<Shadow>(objeto);
        sombra.effectColor = new Color(0.08f, 0.1f, 0.3f, alfa);
        sombra.effectDistance = new Vector2(0f, -distancia);
        sombra.useGraphicAlpha = true;
    }

    // Convierte un panel a pantalla completa en un modal: velo oscuro de
    // fondo y una tarjeta centrada que contiene el formulario.
    private static Transform PrepararModal(Transform panel, float ancho)
    {
        panel.SetAsLastSibling();
        panel.localScale = Vector3.one;
        VerticalLayoutGroup layoutViejo = panel.GetComponent<VerticalLayoutGroup>();
        if (layoutViejo != null)
        {
            Object.DestroyImmediate(layoutViejo);
        }
        Tema.Estirar(Rect(panel));
        panel.GetComponent<Image>().sprite = null;
        panel.GetComponent<Image>().color = Tema.Velo;

        GameObject tarjeta = Hijo(panel, "Tarjeta", typeof(Image), typeof(Shadow));
        EstiloTarjeta(tarjeta, 24);
        Anclar(Rect(tarjeta), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ancho, 0f), new Vector2(0.5f, 0.5f));
        Vertical(tarjeta, 40, 36, 14f, TextAnchor.UpperLeft);
        AjustarAlto(tarjeta.transform, -1f);
        return tarjeta.transform;
    }

    // Campo y botón nuevos (para modales que no existían en la escena), con
    // la estructura estándar de TextMeshPro; el estilo lo ponen Campo/Boton.
    private static Transform NuevoCampo(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null) return existente;
        GameObject campo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        return Adoptar(campo, padre, nombre);
    }

    private static Transform NuevoBoton(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null) return existente;
        GameObject boton = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        return Adoptar(boton, padre, nombre);
    }

    private static Transform Adoptar(GameObject objeto, Transform padre, string nombre)
    {
        objeto.name = nombre;
        objeto.transform.SetParent(padre, false);
        foreach (Transform t in objeto.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = padre.gameObject.layer;
        }
        return objeto.transform;
    }

    private static Transform FilaBotones(Transform padre, Transform izquierda, Transform derecha)
    {
        GameObject fila = Hijo(padre, "FilaBotones");
        HorizontalLayoutGroup h = Horizontal(fila, 0, 0, 12f, TextAnchor.MiddleCenter);
        h.childControlWidth = true;
        h.childForceExpandWidth = true;
        Layout(fila).preferredHeight = AltoBoton;
        izquierda.SetParent(fila.transform, false);
        derecha.SetParent(fila.transform, false);
        Ordenar(fila.transform, izquierda, derecha);
        return fila.transform;
    }

    // Recuadro de error (AvisoError): envuelve el TextoError que ya existía
    // y se asigna en el campo avisoError de la pantalla.
    private static Transform CrearAvisoError(Transform padre, string panelDelTexto, MonoBehaviour pantalla,
        Transform raizBusqueda = null, string campo = "avisoError")
    {
        Transform raiz = raizBusqueda != null ? raizBusqueda : padre;
        Transform textoError = BuscarTextoError(raiz, panelDelTexto);

        GameObject aviso = Hijo(padre, "AvisoError", typeof(Image));
        Image fondo = aviso.GetComponent<Image>();
        fondo.sprite = Tema.Redondeado(12);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.PeligroSuave;
        HorizontalLayoutGroup fila = Horizontal(aviso, 14, 12, 12f, TextAnchor.UpperLeft);
        fila.childControlWidth = true;
        fila.childControlHeight = true;

        GameObject circulo = Hijo(aviso.transform, "Icono", typeof(Image));
        circulo.GetComponent<Image>().sprite = Tema.Circulo;
        circulo.GetComponent<Image>().color = Tema.Peligro;
        LayoutElement circuloLayout = Layout(circulo);
        circuloLayout.preferredWidth = circuloLayout.minWidth = 22f;
        circuloLayout.preferredHeight = circuloLayout.minHeight = 22f;
        TMP_Text signo = TextoEn(circulo.transform, "Signo", "!", Tema.FuenteTitulo, 14f, Color.white, TextAlignmentOptions.Center);
        Tema.Estirar(signo.rectTransform);
        signo.margin = new Vector4(0f, 2f, 0f, 0f);

        textoError.SetParent(aviso.transform, false);
        textoError.gameObject.SetActive(true);
        TMP_Text texto = textoError.GetComponent<TMP_Text>();
        EstiloTexto(texto, Tema.FuenteTextoFuerte, 15f, Tema.PeligroOscuro, TextAlignmentOptions.TopLeft);
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.text = "Mensaje de error";
        Layout(textoError.gameObject).flexibleWidth = 1f;
        Ordenar(aviso.transform, circulo.transform, textoError);

        AvisoError componente = Componente<AvisoError>(aviso);
        Asignar(componente, "texto", texto);
        Asignar(pantalla, campo, componente);
        aviso.SetActive(false);
        return aviso.transform;
    }

    private static Transform BuscarTextoError(Transform raiz, string panelDelTexto)
    {
        Transform directo = raiz.Find("TextoError");
        if (directo != null) return directo;
        Transform enAviso = raiz.Find("AvisoError/TextoError");
        if (enAviso != null) return enAviso;
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "TextoError") return t;
        }
        throw new Exception($"No se encontró TextoError en {panelDelTexto}");
    }

    private static Transform Grupo(Transform padre, string nombre, string etiqueta, Transform campo, string placeholder)
    {
        GameObject grupo = Hijo(padre, nombre);
        Vertical(grupo, 0, 0, 8f, TextAnchor.UpperLeft);
        Transform textoEtiqueta = Etiqueta(grupo.transform, "Etiqueta", etiqueta, Tema.Texto, 14.5f);
        campo.SetParent(grupo.transform, false);
        if (campo.GetComponent<TMP_InputField>() != null)
        {
            Campo(campo, placeholder);
        }
        else if (campo.GetComponent<TMP_Dropdown>() != null)
        {
            Desplegable(campo.GetComponent<TMP_Dropdown>());
        }
        Ordenar(grupo.transform, textoEtiqueta, campo);
        return grupo.transform;
    }

    // Igual que Grupo, pero reutiliza la etiqueta que ya había en la escena.
    private static Transform GrupoConEtiqueta(Transform padre, string nombre, Transform etiquetaExistente, string etiqueta, Transform campo, string placeholder)
    {
        GameObject grupo = Hijo(padre, nombre);
        Vertical(grupo, 0, 0, 8f, TextAnchor.UpperLeft);
        etiquetaExistente.SetParent(grupo.transform, false);
        TMP_Text textoEtiqueta = etiquetaExistente.GetComponent<TMP_Text>();
        EstiloTexto(textoEtiqueta, Tema.FuenteTextoFuerte, 14.5f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        textoEtiqueta.text = etiqueta;
        textoEtiqueta.textWrappingMode = TextWrappingModes.NoWrap;
        Layout(etiquetaExistente.gameObject).preferredHeight = 20f;
        campo.SetParent(grupo.transform, false);
        Campo(campo, placeholder);
        Ordenar(grupo.transform, etiquetaExistente, campo);
        return grupo.transform;
    }

    private static void Campo(Transform campo, string placeholder)
    {
        TMP_InputField input = campo.GetComponent<TMP_InputField>();
        Image fondo = campo.GetComponent<Image>();
        fondo.sprite = Tema.RedondeadoConBorde(10);
        fondo.type = Image.Type.Sliced;
        fondo.color = Color.white;
        LayoutElement layout = Layout(campo.gameObject);
        layout.preferredHeight = layout.minHeight = AltoCampo;

        ColorBlock colores = input.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(0.97f, 0.97f, 1f, 1f);
        colores.selectedColor = new Color(0.95f, 0.94f, 1f, 1f);
        colores.pressedColor = new Color(0.95f, 0.94f, 1f, 1f);
        colores.disabledColor = new Color(0.9f, 0.9f, 0.92f, 1f);
        input.colors = colores;
        input.customCaretColor = true;
        input.caretColor = Tema.Primario;
        input.caretWidth = 2;
        input.selectionColor = new Color(Tema.Primario.r, Tema.Primario.g, Tema.Primario.b, 0.25f);

        RectTransform area = input.textViewport;
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(16f, 6f);
        area.offsetMax = new Vector2(-16f, -6f);

        TMP_Text texto = input.textComponent;
        EstiloTexto(texto, Tema.FuenteTexto, 17f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        input.pointSize = 17f;
        if (input.placeholder is TMP_Text textoPlaceholder)
        {
            EstiloTexto(textoPlaceholder, Tema.FuenteTexto, 17f, Tema.TextoTenue, TextAlignmentOptions.MidlineLeft);
            textoPlaceholder.fontStyle = FontStyles.Normal;
            if (placeholder != null)
            {
                textoPlaceholder.text = placeholder;
            }
        }
    }

    private static void Desplegable(TMP_Dropdown desplegable)
    {
        Image fondo = desplegable.GetComponent<Image>();
        fondo.sprite = Tema.RedondeadoConBorde(10);
        fondo.type = Image.Type.Sliced;
        fondo.color = Color.white;
        LayoutElement layout = Layout(desplegable.gameObject);
        layout.preferredHeight = layout.minHeight = AltoCampo;

        TMP_Text etiqueta = desplegable.captionText;
        EstiloTexto(etiqueta, Tema.FuenteTexto, 17f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        etiqueta.rectTransform.offsetMin = new Vector2(16f, 6f);
        etiqueta.rectTransform.offsetMax = new Vector2(-44f, -6f);
        Transform flecha = desplegable.transform.Find("Arrow");
        if (flecha != null)
        {
            flecha.GetComponent<Image>().color = Tema.TextoSecundario;
            Anclar(Rect(flecha), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(18f, 18f), new Vector2(1f, 0.5f));
        }

        RectTransform plantilla = desplegable.template;
        Image fondoPlantilla = plantilla.GetComponent<Image>();
        fondoPlantilla.sprite = Tema.RedondeadoConBorde(12);
        fondoPlantilla.type = Image.Type.Sliced;
        fondoPlantilla.color = Color.white;
        plantilla.sizeDelta = new Vector2(plantilla.sizeDelta.x, 260f);
        plantilla.anchoredPosition = new Vector2(0f, -6f);
        Sombra(plantilla.gameObject, 0.15f, 8f);

        Transform item = plantilla.Find("Viewport/Content/Item");
        Rect(item).sizeDelta = new Vector2(Rect(item).sizeDelta.x, 44f);
        Transform contenido = plantilla.Find("Viewport/Content");
        Rect(contenido).sizeDelta = new Vector2(Rect(contenido).sizeDelta.x, 52f);
        Image fondoItem = item.Find("Item Background").GetComponent<Image>();
        fondoItem.sprite = Tema.Redondeado(8);
        fondoItem.type = Image.Type.Sliced;
        Toggle toggleItem = item.GetComponent<Toggle>();
        ColorBlock colores = toggleItem.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = Tema.PrimarioSuave;
        colores.selectedColor = Tema.PrimarioSuave;
        colores.pressedColor = Tema.PrimarioSuave;
        toggleItem.colors = colores;
        Rect(item.Find("Item Background")).offsetMin = new Vector2(4f, 2f);
        Rect(item.Find("Item Background")).offsetMax = new Vector2(-4f, -2f);
        item.Find("Item Checkmark").GetComponent<Image>().color = Tema.Primario;
        TMP_Text etiquetaItem = item.Find("Item Label").GetComponent<TMP_Text>();
        EstiloTexto(etiquetaItem, Tema.FuenteTexto, 15.5f, Tema.Texto, TextAlignmentOptions.MidlineLeft);
        Transform barra = plantilla.Find("Scrollbar");
        if (barra != null)
        {
            barra.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            Transform manija = barra.Find("Sliding Area/Handle");
            if (manija != null)
            {
                manija.GetComponent<Image>().sprite = Tema.Redondeado(6);
                manija.GetComponent<Image>().color = Tema.Borde;
            }
        }
    }

    // Casilla moderna: recuadro con borde y, al tildarla, un cuadrado
    // relleno con el color primario y la tilde blanca.
    private static void Casilla(Toggle toggle)
    {
        Image recuadro = toggle.transform.Find("Background").GetComponent<Image>();
        recuadro.sprite = Tema.RedondeadoConBorde(6);
        recuadro.type = Image.Type.Sliced;
        recuadro.color = Color.white;
        RectTransform recuadroRect = recuadro.rectTransform;
        recuadroRect.sizeDelta = new Vector2(24f, 24f);
        if (toggle.GetComponent<Image>() == null)
        {
            // Sin tinte al pasar el puntero: oscurecería el recuadro y dejaría
            // ver la tilde blanca de la casilla destildada (ver abajo).
            toggle.targetGraphic = recuadro;
            toggle.transition = Selectable.Transition.None;
        }
        Anclar(recuadroRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 24f), new Vector2(0f, 0.5f));

        Image relleno = recuadro.transform.Find("Checkmark").GetComponent<Image>();
        relleno.sprite = Tema.Redondeado(6);
        relleno.type = Image.Type.Sliced;
        relleno.color = Tema.Primario;
        Tema.Estirar(relleno.rectTransform);

        // La tilde es hija del relleno. Con la casilla destildada el relleno
        // se apaga, y la tilde blanca queda sobre el recuadro blanco: no se ve.
        Image tilde = ImagenEn(relleno.transform, "Tilde", SpriteUI("Tilde"));
        tilde.color = Color.white;
        Tema.Estirar(tilde.rectTransform, 2f, 2f);
        toggle.graphic = relleno;
    }

    private static void BotonInfoCircular(Transform info)
    {
        Image circulo = info.GetComponent<Image>();
        circulo.sprite = Tema.Circulo;
        circulo.color = Tema.PrimarioSuave;
        LayoutElement layout = Layout(info.gameObject);
        layout.preferredWidth = layout.minWidth = 24f;
        layout.preferredHeight = 24f;
        TMP_Text letra = Buscar(info, "Text (TMP)").GetComponent<TMP_Text>();
        EstiloTexto(letra, Tema.FuenteTitulo, 14f, Tema.Primario, TextAlignmentOptions.Center);
        Tema.Estirar(letra.rectTransform);
        letra.margin = new Vector4(0f, 2f, 0f, 0f);

        // El tooltip: tarjeta oscura a la derecha del ícono.
        Transform panelInfo = Buscar(info, "PanelInfo");
        Image fondo = panelInfo.GetComponent<Image>();
        fondo.sprite = Tema.Redondeado(12);
        fondo.type = Image.Type.Sliced;
        fondo.color = Tema.Oscuro;
        Sombra(panelInfo.gameObject, 0.25f, 8f);
        Anclar(Rect(panelInfo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(12f, 0f), new Vector2(360f, 0f), new Vector2(0f, 0.5f));
        Vertical(panelInfo.gameObject, 18, 16, 0f, TextAnchor.UpperLeft);
        AjustarAlto(panelInfo, -1f);
        Canvas canvasInfo = Componente<Canvas>(panelInfo.gameObject);
        canvasInfo.overrideSorting = true;
        canvasInfo.sortingOrder = 5;
        TMP_Text texto = Buscar(panelInfo, "Text (TMP)").GetComponent<TMP_Text>();
        EstiloTexto(texto, Tema.FuenteTexto, 14.5f, Color.white, TextAlignmentOptions.MidlineLeft);
        texto.lineSpacing = 4f;
        texto.textWrappingMode = TextWrappingModes.Normal;
        panelInfo.gameObject.SetActive(false);
    }

    private static Transform IconoAlerta(Transform padre, string nombre, string simbolo, Color fondo, Color color)
    {
        GameObject contenedor = Hijo(padre, nombre);
        Layout(contenedor).preferredHeight = 52f;
        GameObject circulo = Hijo(contenedor.transform, "Circulo", typeof(Image));
        circulo.GetComponent<Image>().sprite = Tema.Circulo;
        circulo.GetComponent<Image>().color = fondo;
        Anclar(Rect(circulo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(52f, 52f), new Vector2(0f, 0.5f));
        TMP_Text texto = TextoEn(circulo.transform, "Simbolo", simbolo, Tema.FuenteTitulo, 26f, color, TextAlignmentOptions.Center);
        Tema.Estirar(texto.rectTransform);
        texto.margin = new Vector4(0f, 3f, 0f, 0f);
        return contenedor.transform;
    }

    // Lista con desplazamiento: el contenido crece hacia abajo y se recorta
    // en el viewport.
    private static Transform ListaDesplazable(Transform padre, string nombre, Transform contenido)
    {
        GameObject lista = Hijo(padre, nombre, typeof(ScrollRect));
        GameObject viewport = Hijo(lista.transform, "Viewport", typeof(Image), typeof(RectMask2D));
        Tema.Estirar(Rect(viewport));
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        contenido.SetParent(viewport.transform, false);
        RectTransform contenidoRect = Rect(contenido);
        contenidoRect.anchorMin = new Vector2(0f, 1f);
        contenidoRect.anchorMax = new Vector2(1f, 1f);
        contenidoRect.pivot = new Vector2(0.5f, 1f);
        contenidoRect.anchoredPosition = Vector2.zero;
        contenidoRect.sizeDelta = Vector2.zero;
        ContentSizeFitter ajuste = Componente<ContentSizeFitter>(contenido.gameObject);
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        if (contenido.GetComponent<TMP_Text>() == null)
        {
            VerticalLayoutGroup v = Componente<VerticalLayoutGroup>(contenido.gameObject);
            v.spacing = 8f;
            v.padding = new RectOffset(0, 6, 0, 0);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
        }

        ScrollRect scroll = lista.GetComponent<ScrollRect>();
        scroll.viewport = Rect(viewport);
        scroll.content = contenidoRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return lista.transform;
    }

    private static Transform Pildora(Transform padre, string nombre, string texto, Color fondo, Color colorTexto, float tamanio, bool fuerte = false)
    {
        GameObject pildora = Hijo(padre, nombre, typeof(Image));
        Image imagen = pildora.GetComponent<Image>();
        imagen.sprite = Tema.Redondeado(fuerte ? 8 : 20);
        imagen.type = Image.Type.Sliced;
        imagen.color = fondo;
        imagen.raycastTarget = false;
        LayoutElement layout = Layout(pildora);
        layout.preferredHeight = 34f;
        TMP_Text t = TextoEn(pildora.transform, "Texto", texto, fuerte ? Tema.FuenteTextoFuerte : Tema.FuenteTextoFuerte, tamanio, colorTexto, TextAlignmentOptions.Center);
        Tema.Estirar(t.rectTransform, 12f, 0f);
        t.characterSpacing = fuerte ? 0f : 2f;
        return pildora.transform;
    }

    private static void Brillo(Transform padre, string nombre, Vector2 ancla, float tamanio, Color color)
    {
        GameObject brillo = Hijo(padre, nombre, typeof(Image));
        Anclar(Rect(brillo), ancla, ancla, Vector2.zero, new Vector2(tamanio, tamanio), new Vector2(0.5f, 0.5f));
        Image imagen = brillo.GetComponent<Image>();
        imagen.sprite = SpriteUI("Brillo");
        imagen.color = color;
        imagen.raycastTarget = false;
    }

    private static void LineaBorde(Transform padre, string nombre, bool abajo)
    {
        GameObject linea = Hijo(padre, nombre, typeof(Image), typeof(LayoutElement));
        linea.GetComponent<LayoutElement>().ignoreLayout = true;
        linea.GetComponent<Image>().color = Tema.Borde;
        linea.GetComponent<Image>().raycastTarget = false;
        RectTransform rect = Rect(linea);
        if (abajo)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(1f, 0f);
        }
        rect.anchoredPosition = Vector2.zero;
    }

    private static Transform Separador(Transform padre, string nombre, string texto)
    {
        Transform t = Parrafo(padre, nombre, texto, 15f);
        t.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        t.GetComponent<TMP_Text>().color = Tema.TextoTenue;
        return t;
    }

    // --- Textos ---

    private static Transform Titulo(Transform padre, string nombre, string texto, float tamanio)
    {
        TMP_Text t = TextoEn(padre, nombre, texto, Tema.FuenteTitulo, tamanio, Tema.Texto, TextAlignmentOptions.TopLeft);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = -28f;
        return t.transform;
    }

    private static Transform Subtitulo(Transform padre, string nombre, string texto)
    {
        TMP_Text t = TextoEn(padre, nombre, texto, Tema.FuenteSubtitulo, Tema.TamanioSubtitulo, Tema.Texto, TextAlignmentOptions.TopLeft);
        return t.transform;
    }

    private static Transform Parrafo(Transform padre, string nombre, string texto, float tamanio = 17f)
    {
        TMP_Text t = TextoEn(padre, nombre, texto, Tema.FuenteTexto, tamanio, Tema.TextoSecundario, TextAlignmentOptions.TopLeft);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = 4f;
        return t.transform;
    }

    private static Transform Etiqueta(Transform padre, string nombre, string texto, Color color, float tamanio)
    {
        TMP_Text t = TextoEn(padre, nombre, texto, Tema.FuenteTextoFuerte, tamanio, color, TextAlignmentOptions.MidlineLeft);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.characterSpacing = texto == texto.ToUpperInvariant() ? 6f : 0f;
        Layout(t.gameObject).preferredHeight = tamanio + 6f;
        return t.transform;
    }

    private static TMP_Text TextoEn(Transform padre, string nombre, string contenido, TMP_FontAsset fuente, float tamanio, Color color, TextAlignmentOptions alineacion)
    {
        GameObject go = Hijo(padre, nombre, typeof(TextMeshProUGUI));
        TMP_Text t = go.GetComponent<TMP_Text>();
        t.text = contenido;
        EstiloTexto(t, fuente, tamanio, color, alineacion);
        return t;
    }

    private static void EstiloTexto(TMP_Text t, TMP_FontAsset fuente, float tamanio, Color color, TextAlignmentOptions alineacion)
    {
        t.font = fuente;
        t.fontSharedMaterial = fuente.material;
        t.fontSize = tamanio;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = alineacion;
        t.fontStyle = FontStyles.Normal;
        t.raycastTarget = false;
        t.margin = Vector4.zero;
    }

    private static Image ImagenEn(Transform padre, string nombre, Sprite sprite)
    {
        GameObject go = Hijo(padre, nombre, typeof(Image));
        Image imagen = go.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = Color.white;
        imagen.raycastTarget = false;
        return imagen;
    }

    // --- Botones ---

    private static void Boton(Transform boton, Tema.EstiloBoton estilo, string texto, float alto, float ancho = -1f)
    {
        Button componente = boton.GetComponent<Button>();
        Tema.AplicarEstiloBoton(componente, estilo);
        TMP_Text t = boton.GetComponentInChildren<TMP_Text>(true);
        t.text = texto;
        Tema.Estirar(t.rectTransform, 10f, 0f);
        t.margin = Vector4.zero;
        LayoutElement layout = Layout(boton.gameObject);
        layout.preferredHeight = layout.minHeight = alto;
        if (ancho > 0f)
        {
            layout.preferredWidth = layout.minWidth = ancho;
        }
        Rect(boton).sizeDelta = new Vector2(ancho > 0f ? ancho : Rect(boton).sizeDelta.x, alto);
    }

    private static void AlinearTextoBoton(Transform boton, TextAlignmentOptions alineacion, float margen)
    {
        TMP_Text t = boton.GetComponentInChildren<TMP_Text>(true);
        t.alignment = alineacion;
        Tema.Estirar(t.rectTransform, margen, 0f);
    }

    // --- Eventos persistentes (los que se cargan en el Inspector) ---

    // Saca las llamadas a SetActive sobre "objetivo" del onClick de un botón.
    private static void QuitarSetActive(Button boton, GameObject objetivo)
    {
        SerializedObject so = new SerializedObject(boton);
        SerializedProperty llamadas = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        for (int i = llamadas.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty llamada = llamadas.GetArrayElementAtIndex(i);
            bool esSetActive = llamada.FindPropertyRelative("m_MethodName").stringValue == "SetActive";
            if (esSetActive && llamada.FindPropertyRelative("m_Target").objectReferenceValue == objetivo)
            {
                llamadas.DeleteArrayElementAtIndex(i);
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Cambia sobre qué objeto actúa un SetActive del onClick de un botón.
    private static void CambiarDestinoSetActive(Button boton, GameObject anterior, GameObject nuevo)
    {
        SerializedObject so = new SerializedObject(boton);
        SerializedProperty llamadas = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        for (int i = 0; i < llamadas.arraySize; i++)
        {
            SerializedProperty llamada = llamadas.GetArrayElementAtIndex(i);
            SerializedProperty objetivo = llamada.FindPropertyRelative("m_Target");
            if (llamada.FindPropertyRelative("m_MethodName").stringValue == "SetActive" && objetivo.objectReferenceValue == anterior)
            {
                objetivo.objectReferenceValue = nuevo;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- Jerarquía y layout ---

    // Busca por ruta ("Panel/Hijo"), incluidos objetos apagados, comparando
    // nombres sin espacios sobrantes (había un "BotonEliminarCuenta ").
    private static Transform Buscar(Transform raiz, string ruta)
    {
        Transform actual = raiz;
        foreach (string parte in ruta.Split('/'))
        {
            Transform encontrado = null;
            foreach (Transform t in actual.GetComponentsInChildren<Transform>(true))
            {
                if (t != actual && t.name.Trim() == parte && (encontrado == null || Profundidad(t) < Profundidad(encontrado)))
                {
                    encontrado = t;
                }
            }
            actual = encontrado ?? throw new Exception($"[AplicarDiseno] No se encontró '{parte}' en '{ruta}' (desde {raiz.name})");
        }
        return actual;
    }

    private static int Profundidad(Transform t)
    {
        int profundidad = 0;
        while (t.parent != null)
        {
            profundidad++;
            t = t.parent;
        }
        return profundidad;
    }

    // Hijo directo con ese nombre; si no existe, lo crea con esos componentes.
    private static GameObject Hijo(Transform padre, string nombre, params Type[] componentes)
    {
        Transform existente = padre.Find(nombre);
        GameObject go = existente != null ? existente.gameObject : new GameObject(nombre, typeof(RectTransform));
        if (existente == null)
        {
            go.transform.SetParent(padre, false);
            go.layer = padre.gameObject.layer;
        }
        foreach (Type tipo in componentes)
        {
            if (go.GetComponent(tipo) == null)
            {
                go.AddComponent(tipo);
            }
        }
        return go;
    }

    private static T Componente<T>(GameObject go) where T : Component
    {
        T componente = go.GetComponent<T>();
        return componente != null ? componente : go.AddComponent<T>();
    }

    private static LayoutElement Layout(GameObject go) => Componente<LayoutElement>(go);

    private static RectTransform Rect(Component c) => (RectTransform)c.transform;
    private static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

    private static void Anclar(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax, Vector2 posicion, Vector2 tamanio, Vector2? pivote = null)
    {
        rect.anchorMin = anclaMin;
        rect.anchorMax = anclaMax;
        rect.pivot = pivote ?? anclaMin;
        rect.sizeDelta = tamanio;
        rect.anchoredPosition = posicion;
    }

    // Pone los hijos en ese orden, al final del padre.
    private static void Ordenar(Transform padre, params Transform[] hijos)
    {
        foreach (Transform hijo in hijos)
        {
            hijo.SetParent(padre, false);
            hijo.SetAsLastSibling();
            // Algunos paneles venían con escala distinta de 1 en la escena.
            hijo.localScale = Vector3.one;
        }
    }

    private static Transform Espacio(Transform padre, string nombre, float alto)
    {
        GameObject espacio = Hijo(padre, nombre);
        LayoutElement layout = Layout(espacio);
        layout.preferredHeight = alto;
        layout.minHeight = alto;
        return espacio.transform;
    }

    private static VerticalLayoutGroup Vertical(GameObject go, int padH, int padV, float espacio, TextAnchor alineacion)
    {
        VerticalLayoutGroup v = Componente<VerticalLayoutGroup>(go);
        v.padding = new RectOffset(padH, padH, padV, padV);
        v.spacing = espacio;
        v.childAlignment = alineacion;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        v.childScaleHeight = false;
        v.childScaleWidth = false;
        return v;
    }

    private static HorizontalLayoutGroup Horizontal(GameObject go, int padH, int padV, float espacio, TextAnchor alineacion)
    {
        HorizontalLayoutGroup h = Componente<HorizontalLayoutGroup>(go);
        h.padding = new RectOffset(padH, padH, padV, padV);
        h.spacing = espacio;
        h.childAlignment = alineacion;
        h.childControlWidth = false;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        return h;
    }

    // Alto según el contenido (ContentSizeFitter); con -1 no fija ancho.
    private static void AjustarAlto(Transform t, float ancho)
    {
        ContentSizeFitter ajuste = Componente<ContentSizeFitter>(t.gameObject);
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private static void Asignar(Object objeto, string campo, object valor)
    {
        SerializedObject so = new SerializedObject(objeto);
        SerializedProperty propiedad = so.FindProperty(campo) ?? throw new Exception($"[AplicarDiseno] {objeto.GetType().Name} no tiene el campo '{campo}'");
        switch (valor)
        {
            case Object referencia:
                propiedad.objectReferenceValue = referencia;
                break;
            case string texto:
                propiedad.stringValue = texto;
                break;
            case float numero:
                propiedad.floatValue = numero;
                break;
            case Color color:
                propiedad.colorValue = color;
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- Sprites ---

    private static Sprite SpriteUI(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>($"{CarpetaSprites}/{nombre}.png");

    private static Sprite SpritePieza(string archivo)
    {
        return AssetDatabase.LoadAllAssetsAtPath($"Assets/Sprites/{archivo}.png").OfType<Sprite>().First();
    }

    private static readonly int[] Radios = { 6, 8, 10, 12, 14, 16, 20, 24 };

    // Las formas se generan al doble de resolución (Pixels Per Unit = 200)
    // para que los bordes redondeados se vean nítidos en pantallas grandes.
    private const float PixelesPorUnidad = 200f;

    private static void GenerarSprites()
    {
        Directory.CreateDirectory(CarpetaSprites);
        var bordes = new Dictionary<string, int>();
        foreach (int radio in Radios)
        {
            bordes[$"Redondeado{radio}"] = GuardarRedondeado($"Redondeado{radio}", radio, false);
            bordes[$"Borde{radio}"] = GuardarRedondeado($"Borde{radio}", radio, true);
        }
        GuardarTextura("Circulo", 256, 256, (x, y) => Blanco(Cobertura(DistanciaRedondeado(x, y, 256, 256, 128))));
        GuardarTextura("Degradado", 512, 512, (x, y) => Degradado(x / 511f, y / 511f));
        GuardarTextura("DegradadoLogo", 128, 128, (x, y) =>
        {
            Color c = Degradado(x / 127f, y / 127f);
            c.a = Cobertura(DistanciaRedondeado(x, y, 128, 128, 30));
            return c;
        });
        GuardarTextura("DegradadoCirculo", 128, 128, (x, y) =>
        {
            Color c = Degradado(x / 127f, y / 127f);
            c.a = Cobertura(DistanciaRedondeado(x, y, 128, 128, 64));
            return c;
        });
        GuardarTextura("Puntos", 48, 48, (x, y) => Blanco(Cobertura(Mathf.Sqrt((x + 0.5f - 24f) * (x + 0.5f - 24f) + (y + 0.5f - 24f) * (y + 0.5f - 24f)) - 2.6f)));
        GuardarTextura("Tilde", 64, 64, (x, y) =>
        {
            Vector2 punto = new Vector2(x + 0.5f, y + 0.5f);
            float d = Mathf.Min(DistanciaASegmento(punto, new Vector2(15f, 33f), new Vector2(27f, 21f)),
                DistanciaASegmento(punto, new Vector2(27f, 21f), new Vector2(49f, 43f)));
            return Blanco(Cobertura(d - 4.5f));
        });
        GuardarTextura("Brillo", 256, 256, (x, y) =>
        {
            float d = Mathf.Sqrt((x - 127.5f) * (x - 127.5f) + (y - 127.5f) * (y - 127.5f)) / 128f;
            float a = Mathf.Clamp01(1f - d);
            return Blanco(a * a * a);
        });
        AssetDatabase.Refresh();

        foreach (KeyValuePair<string, int> sprite in bordes)
        {
            ConfigurarSprite($"{CarpetaSprites}/{sprite.Key}.png", PixelesPorUnidad, sprite.Value, TextureWrapMode.Clamp);
        }
        ConfigurarSprite($"{CarpetaSprites}/Circulo.png", PixelesPorUnidad, 0, TextureWrapMode.Clamp);
        ConfigurarSprite($"{CarpetaSprites}/Degradado.png", 100f, 0, TextureWrapMode.Clamp);
        ConfigurarSprite($"{CarpetaSprites}/DegradadoLogo.png", PixelesPorUnidad, 0, TextureWrapMode.Clamp);
        ConfigurarSprite($"{CarpetaSprites}/DegradadoCirculo.png", PixelesPorUnidad, 0, TextureWrapMode.Clamp);
        ConfigurarSprite($"{CarpetaSprites}/Puntos.png", PixelesPorUnidad, 0, TextureWrapMode.Repeat);
        ConfigurarSprite($"{CarpetaSprites}/Brillo.png", 100f, 0, TextureWrapMode.Clamp);
        ConfigurarSprite($"{CarpetaSprites}/Tilde.png", PixelesPorUnidad, 0, TextureWrapMode.Clamp);
    }

    // Violeta arriba a la izquierda, azul al medio, cian abajo a la derecha.
    private static Color Degradado(float u, float v)
    {
        float t = Mathf.Clamp01((u + (1f - v)) / 2f);
        Color violeta = Tema.Hex("#6A3BFF");
        Color azul = Tema.Hex("#4F63FF");
        Color cian = Tema.Hex("#00C2E0");
        return t < 0.5f ? Color.Lerp(violeta, azul, t * 2f) : Color.Lerp(azul, cian, (t - 0.5f) * 2f);
    }

    private static float DistanciaASegmento(Vector2 punto, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(punto - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(punto, a + ab * t);
    }

    private static Color Blanco(float alfa) => new Color(1f, 1f, 1f, alfa);

    private static float Cobertura(float distancia) => Mathf.Clamp01(0.5f - distancia);

    private static int GuardarRedondeado(string nombre, int radioUI, bool conBorde)
    {
        int radio = radioUI * 2;
        int tamanio = radio * 2 + 4;
        Color colorBorde = Tema.Hex("#DCE1EC");
        GuardarTextura(nombre, tamanio, tamanio, (x, y) =>
        {
            float d = DistanciaRedondeado(x, y, tamanio, tamanio, radio);
            Color c = Color.white;
            if (conBorde)
            {
                // Borde de 2 píxeles de textura = 1 unidad de UI.
                c = Color.Lerp(colorBorde, Color.white, Cobertura(d + 2f));
            }
            c.a = Cobertura(d);
            return c;
        });
        return radio + 1;
    }

    // Distancia con signo del centro del píxel al borde de un rectángulo
    // redondeado que ocupa toda la textura (negativa adentro).
    private static float DistanciaRedondeado(int x, int y, int ancho, int alto, float radio)
    {
        float px = x + 0.5f - ancho / 2f;
        float py = y + 0.5f - alto / 2f;
        float qx = Mathf.Abs(px) - (ancho / 2f - radio);
        float qy = Mathf.Abs(py) - (alto / 2f - radio);
        float fueraX = Mathf.Max(qx, 0f);
        float fueraY = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(fueraX * fueraX + fueraY * fueraY) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radio;
    }

    private static void GuardarTextura(string nombre, int ancho, int alto, Func<int, int, Color> pixel)
    {
        var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                textura.SetPixel(x, y, pixel(x, y));
            }
        }
        textura.Apply();
        File.WriteAllBytes($"{CarpetaSprites}/{nombre}.png", textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
    }

    private static void ConfigurarSprite(string ruta, float pixelesPorUnidad, int borde, TextureWrapMode envoltura)
    {
        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = pixelesPorUnidad;
        importador.spriteBorder = new Vector4(borde, borde, borde, borde);
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.filterMode = FilterMode.Bilinear;
        importador.wrapMode = envoltura;
        var ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        importador.SetTextureSettings(ajustes);
        importador.SaveAndReimport();
    }

    // --- Fuentes ---

    private static readonly string[] Fuentes = { "Inter-Regular", "Inter-SemiBold", "Poppins-SemiBold", "Poppins-Bold" };

    // Caracteres que se cargan de antemano en el atlas. El resto se agrega
    // solo cuando aparece (atlas dinámico).
    private const string CaracteresPrecargados =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "¡¿áéíóúÁÉÍÓÚñÑüÜ°±·•—–…“”‘’×←→✓●▶‖";

    private static void GenerarFuentes()
    {
        Directory.CreateDirectory(CarpetaFuentes);
        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        foreach (string nombre in Fuentes)
        {
            string ruta = $"{CarpetaFuentes}/{nombre} SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta) != null) continue;

            Font ttf = AssetDatabase.LoadAssetAtPath<Font>($"{CarpetaTtf}/{nombre}.ttf");
            TMP_FontAsset fuente = TMP_FontAsset.CreateFontAsset(ttf, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fuente.name = $"{nombre} SDF";
            // Sin las tablas de kerning de las fuentes: la de Poppins separa
            // de más algunos pares ("C omparación"), y la de Inter son 17.000
            // pares que hacen pesar al asset 7 MB. En textos de interfaz la
            // diferencia no se nota.
            fuente.getFontFeatures = false;
            fuente.fontFeatureTable.glyphPairAdjustmentRecords.Clear();
            AssetDatabase.CreateAsset(fuente, ruta);
            fuente.atlasTextures[0].name = $"{nombre} Atlas";
            AssetDatabase.AddObjectToAsset(fuente.atlasTextures[0], fuente);
            fuente.material.name = $"{nombre} Material";
            AssetDatabase.AddObjectToAsset(fuente.material, fuente);

            fuente.TryAddCharacters(CaracteresPrecargados, out string faltantes);
            if (!string.IsNullOrEmpty(faltantes))
            {
                Debug.Log($"[AplicarDiseno] {nombre} no tiene: {faltantes} (se usa LiberationSans)");
            }
            for (int i = 1; i < fuente.atlasTextures.Length; i++)
            {
                if (!AssetDatabase.IsSubAsset(fuente.atlasTextures[i]))
                {
                    fuente.atlasTextures[i].name = $"{nombre} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(fuente.atlasTextures[i], fuente);
                }
            }
            fuente.fallbackFontAssetTable = new List<TMP_FontAsset> { liberation };
            EditorUtility.SetDirty(fuente);
        }
        AssetDatabase.SaveAssets();
        TMP_FontAsset inter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{CarpetaFuentes}/Inter-SemiBold SDF.asset");
        foreach (char c in "■▮‖⏸Ⅱǁǀ⏯")
        {
            Debug.Log($"[AplicarDiseno] glifo {c} (U+{(int)c:X4}): Inter={inter.HasCharacter(c, false, true)} Liberation={liberation.HasCharacter(c, false, true)}");
        }

        // Fuente por defecto de TextMeshPro: la usan los textos que se crean
        // por código sin asignarles una (etiquetas del gráfico, rampas).
        TMP_Settings ajustes = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        SerializedObject so = new SerializedObject(ajustes);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{CarpetaFuentes}/Inter-Regular SDF.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
}
