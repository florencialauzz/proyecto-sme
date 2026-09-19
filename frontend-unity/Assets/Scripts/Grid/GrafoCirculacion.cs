using System;
using System.Collections.Generic;

namespace Sme.Grid
{
    // Parte de editor/grafo-circulacion.md que ya se puede calcular sin
    // aristas ni alcanzabilidad: conexión entre celdas contiguas y el rol
    // (segmento/cruce) que se deriva de esa cuenta. El resto del grafo
    // (aristas, alcanzabilidad, RF-20) todavía no está implementado.
    public static class GrafoCirculacion
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

        // Calle, Entrada y Salida guardan direccion y participan del grafo.
        // Rampa se suma acá cuando exista (Iteración 3) — Plaza y
        // ZonaBicicletasMotos nunca, se enganchan por su boca pero el
        // vehículo no las atraviesa.
        public static bool EsCeldaDeCirculacion(TipoPieza? tipo)
        {
            return tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA;
        }

        // Colineal con el eje norte-sur o este-oeste que une a la celda con
        // esa vecina, para cualquiera de los dos lados del eje.
        private static bool EsColinealConEje(CaraAcceso flecha, CaraAcceso direccionAlVecino)
        {
            bool ejeEsNorteSur = direccionAlVecino == CaraAcceso.NORTE || direccionAlVecino == CaraAcceso.SUR;
            bool flechaEsNorteSur = flecha == CaraAcceso.NORTE || flecha == CaraAcceso.SUR;
            return ejeEsNorteSur == flechaEsNorteSur;
        }

        // Dos celdas de circulación contiguas están conectadas si al menos
        // una de las dos flechas guardadas es colineal con el eje que las
        // une — simétrico, sin importar si alguna es cruce.
        private static bool EstanConectadas(CeldaGrilla celda, CeldaGrilla vecina, CaraAcceso direccionAlVecino)
        {
            if (!EsCeldaDeCirculacion(celda.Tipo) || !EsCeldaDeCirculacion(vecina.Tipo)) return false;

            bool celdaColineal = EsColinealConEje(celda.Direccion.Value, direccionAlVecino);
            bool vecinaColineal = EsColinealConEje(vecina.Direccion.Value, direccionAlVecino);
            return celdaColineal || vecinaColineal;
        }

        // Qué caras de la celda están conectadas — el autotiling (RF-16,
        // editor/catalogo-piezas.md) necesita saber cuáles, no solo cuántas,
        // para elegir la rotación del sprite.
        public static CaraAcceso[] ObtenerDireccionesConectadas(CeldaGrilla celda)
        {
            if (!EsCeldaDeCirculacion(celda.Tipo)) return Array.Empty<CaraAcceso>();

            var conectadas = new List<CaraAcceso>();
            foreach (CaraAcceso direccion in Direcciones)
            {
                (int deltaFila, int deltaColumna) = GrillaModelo.Delta(direccion);
                CeldaGrilla vecina = GrillaModelo.ObtenerCelda(celda.Fila + deltaFila, celda.Columna + deltaColumna);
                if (vecina != null && EstanConectadas(celda, vecina, direccion))
                {
                    conectadas.Add(direccion);
                }
            }
            return conectadas.ToArray();
        }

        public static int ContarConexiones(CeldaGrilla celda)
        {
            return ObtenerDireccionesConectadas(celda).Length;
        }

        // Entrada y Salida nunca son cruce, tengan las vecinas que tengan
        // (Rampa tampoco, cuando exista).
        public static bool EsCruce(CeldaGrilla celda)
        {
            return celda.Tipo == TipoPieza.CALLE && ContarConexiones(celda) >= 3;
        }
    }
}
