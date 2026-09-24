using System;
using System.Collections.Generic;

namespace Sme.Grid
{
    // editor/grafo-circulacion.md del lado de Unity: conexión entre celdas
    // contiguas, el rol (segmento/cruce) que se deriva de esa cuenta, y las
    // aristas dirigidas por donde puede pasar el vehículo (RF-20). Todo se
    // calcula en el momento a partir de GrillaModelo — no hay nada guardado.
    //
    // Java tiene su propia copia de estas reglas para la simulación
    // (arquitectura/decisiones.md): son implementaciones separadas a
    // propósito, no hay que unificarlas.
    public static class GrafoCirculacion
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

        // Calle, Entrada, Salida y Rampa guardan direccion y participan del
        // grafo — Plaza y ZonaBicicletasMotos nunca, se enganchan por su boca
        // pero el vehículo no las atraviesa. La Escalera tampoco: es solo
        // para el peatón.
        public static bool EsCeldaDeCirculacion(TipoPieza? tipo)
        {
            return tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA
                || tipo == TipoPieza.RAMPA;
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
                CeldaGrilla vecina = GrillaModelo.ObtenerCelda(celda.Piso, celda.Fila + deltaFila, celda.Columna + deltaColumna);
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

        // Entrada, Salida y Rampa nunca son cruce, tengan las vecinas que
        // tengan.
        public static bool EsCruce(CeldaGrilla celda)
        {
            return celda.Tipo == TipoPieza.CALLE && ContarConexiones(celda) >= 3;
        }

        // --- Aristas (RF-20) ---

        // Por qué caras puede salir el vehículo hacia una vecina del mismo
        // piso (editor/grafo-circulacion.md, "Salidas"):
        // - Calle segmento: solo la cara de su flecha.
        // - Calle cruce: cualquier cara conectada (perdió la flecha).
        // - Entrada: la cara de su flecha, que apunta hacia adentro.
        // - Salida: ninguna hacia la grilla — su flecha apunta afuera, y esa
        //   cara cuenta como salida válida aparte (TieneSalidaValida).
        // - Rampa, celda del piso de entrada: ninguna horizontal — su única
        //   salida es el salto al otro piso (ParDeRampa).
        // - Rampa, celda del piso de salida: la cara de su flecha.
        private static CaraAcceso[] CarasDeSalida(CeldaGrilla celda)
        {
            switch (celda.Tipo)
            {
                case TipoPieza.CALLE:
                    return EsCruce(celda)
                        ? ObtenerDireccionesConectadas(celda)
                        : new[] { celda.Direccion.Value };
                case TipoPieza.ENTRADA:
                    return new[] { celda.Direccion.Value };
                case TipoPieza.RAMPA:
                    return celda.EsEntradaDeRampa
                        ? Array.Empty<CaraAcceso>()
                        : new[] { celda.Direccion.Value };
                default:
                    return Array.Empty<CaraAcceso>();
            }
        }

        // Si la celda deja entrar al vehículo que llega por esa cara
        // (editor/grafo-circulacion.md, "Acepta entrada"). Quien llama ya
        // verificó que las dos celdas están conectadas.
        // - Calle segmento: por cualquier cara que no sea la de su flecha.
        // - Calle cruce: por cualquiera.
        // - Entrada: por ninguna (es fuente: sin esto se podría atravesar la
        //   boca del estacionamiento en contramano).
        // - Salida: por cualquiera.
        // - Rampa, piso de entrada: solo por la cara opuesta a la flecha —
        //   el vehículo encara la rampa de frente. Una calle que llega por un
        //   costado o por el frente no tiene arista hacia la rampa.
        // - Rampa, piso de salida: por ninguna — ahí el vehículo solo llega
        //   subiendo o bajando por el salto.
        private static bool AceptaEntradaPor(CeldaGrilla celda, CaraAcceso caraDeLlegada)
        {
            switch (celda.Tipo)
            {
                case TipoPieza.CALLE:
                    return EsCruce(celda) || caraDeLlegada != celda.Direccion.Value;
                case TipoPieza.SALIDA:
                    return true;
                case TipoPieza.RAMPA:
                    return celda.EsEntradaDeRampa && caraDeLlegada == GrillaModelo.Opuesta(celda.Direccion.Value);
                default:
                    return false;
            }
        }

        // La celda de rampa del otro piso que corresponde a esta, si esta es
        // la del piso de entrada: misma posición, un piso arriba si sube o
        // uno abajo si baja. null si no es entrada de rampa.
        public static CeldaGrilla ParDeRampa(CeldaGrilla celda)
        {
            if (celda.Tipo != TipoPieza.RAMPA || !celda.EsEntradaDeRampa) return null;

            int pisoSalida = celda.SentidoVertical == SentidoVertical.SUBE ? celda.Piso + 1 : celda.Piso - 1;
            CeldaGrilla par = GrillaModelo.ObtenerCelda(pisoSalida, celda.Fila, celda.Columna);
            bool esSuPar = par != null && par.Tipo == TipoPieza.RAMPA && !par.EsEntradaDeRampa
                && par.SentidoVertical == celda.SentidoVertical;
            return esSuPar ? par : null;
        }

        // Existe la arista A → B si A y B están conectadas, B está sobre una
        // salida de A, y B acepta la entrada desde A. Además, la celda de
        // entrada de una rampa tiene una arista al salto: su par en el otro
        // piso.
        public static List<CeldaGrilla> Sucesores(CeldaGrilla celda)
        {
            var sucesores = new List<CeldaGrilla>();
            if (!EsCeldaDeCirculacion(celda.Tipo)) return sucesores;

            foreach (CaraAcceso cara in CarasDeSalida(celda))
            {
                (int deltaFila, int deltaColumna) = GrillaModelo.Delta(cara);
                CeldaGrilla vecina = GrillaModelo.ObtenerCelda(celda.Piso, celda.Fila + deltaFila, celda.Columna + deltaColumna);
                if (vecina == null) continue;

                if (EstanConectadas(celda, vecina, cara) && AceptaEntradaPor(vecina, GrillaModelo.Opuesta(cara)))
                {
                    sucesores.Add(vecina);
                }
            }

            CeldaGrilla par = ParDeRampa(celda);
            if (par != null)
            {
                sucesores.Add(par);
            }

            return sucesores;
        }

        // Regla única de salida válida (editor/grafo-circulacion.md): ninguna
        // celda de circulación puede quedar sin al menos una arista de
        // salida. La Salida siempre tiene una (su cara hacia el exterior), y
        // la entrada de una rampa también (el salto, incluido en Sucesores).
        public static bool TieneSalidaValida(CeldaGrilla celda)
        {
            if (celda.Tipo == TipoPieza.SALIDA) return true;
            return Sucesores(celda).Count > 0;
        }

        // Para el mensaje de RF-20: una calle sin salida porque la celda a la
        // que apunta es otra calle que la apunta de vuelta (segmentos
        // enfrentados). Es más accionable decir eso que "no tiene salida".
        public static bool EstaEnfrentada(CeldaGrilla celda)
        {
            if (celda.Tipo != TipoPieza.CALLE || EsCruce(celda)) return false;

            (int deltaFila, int deltaColumna) = GrillaModelo.Delta(celda.Direccion.Value);
            CeldaGrilla vecina = GrillaModelo.ObtenerCelda(celda.Piso, celda.Fila + deltaFila, celda.Columna + deltaColumna);
            return vecina != null && vecina.Tipo == TipoPieza.CALLE && !EsCruce(vecina)
                && vecina.Direccion.Value == GrillaModelo.Opuesta(celda.Direccion.Value);
        }
    }
}
