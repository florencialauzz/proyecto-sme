namespace Sme.Models
{
    // RF-01: catálogo fijo de preguntas de seguridad típicas. El usuario elige
    // una de una picklist en vez de escribir su propia pregunta — evita
    // preguntas ambiguas o mal formuladas, y deja la respuesta como el único
    // campo de texto libre. El backend guarda el texto tal cual (pregunta_seguridad
    // VARCHAR(255) en esquema-bd.md), así que agregar o quitar preguntas acá no
    // requiere ningún cambio de contrato.
    public static class PreguntasSeguridad
    {
        // Lista acotada a propósito mientras se depura el armado del Dropdown en
        // el Editor — dos opciones cortas alcanzan para probar que el mecanismo
        // funciona antes de volver a la lista completa.
        public static readonly string[] Opciones =
        {
            "¿Cuál es tu color favorito?",
            "¿En qué ciudad naciste?"
        };
    }
}
