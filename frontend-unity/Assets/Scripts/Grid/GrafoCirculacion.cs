using System;
using System.Collections.Generic;

namespace Sme.Grid
{
    public static class GrafoCirculacion
    {
        private static readonly CaraAcceso[] Direcciones =
        {
            CaraAcceso.NORTE, CaraAcceso.SUR, CaraAcceso.ESTE, CaraAcceso.OESTE
        };

        public static bool EsCeldaDeCirculacion(TipoPieza? tipo)
        {
            return tipo == TipoPieza.CALLE || tipo == TipoPieza.ENTRADA || tipo == TipoPieza.SALIDA
                || tipo == TipoPieza.RAMPA;
        }

        private static bool EsColinealConEje(CaraAcceso flecha, CaraAcceso direccionAlVecino)
        {
            bool ejeEsNorteSur = direccionAlVecino == CaraAcceso.NORTE || direccionAlVecino == CaraAcceso.SUR;
            bool flechaEsNorteSur = flecha == CaraAcceso.NORTE || flecha == CaraAcceso.SUR;
            return ejeEsNorteSur == flechaEsNorteSur;
        }

        private static bool EstanConectadas(CeldaGrilla celda, CeldaGrilla vecina, CaraAcceso direccionAlVecino)
        {
            if (!EsCeldaDeCirculacion(celda.Tipo) || !EsCeldaDeCirculacion(vecina.Tipo)) return false;

            bool celdaColineal = EsColinealConEje(celda.Direccion.Value, direccionAlVecino);
            bool vecinaColineal = EsColinealConEje(vecina.Direccion.Value, direccionAlVecino);
            return celdaColineal || vecinaColineal;
        }

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

        public static bool EsCruce(CeldaGrilla celda)
        {
            return celda.Tipo == TipoPieza.CALLE && ContarConexiones(celda) >= 3;
        }

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

        public static CeldaGrilla ParDeRampa(CeldaGrilla celda)
        {
            if (celda.Tipo != TipoPieza.RAMPA || !celda.EsEntradaDeRampa) return null;

            int pisoSalida = celda.SentidoVertical == SentidoVertical.SUBE ? celda.Piso + 1 : celda.Piso - 1;
            CeldaGrilla par = GrillaModelo.ObtenerCelda(pisoSalida, celda.Fila, celda.Columna);
            bool esSuPar = par != null && par.Tipo == TipoPieza.RAMPA && !par.EsEntradaDeRampa
                && par.SentidoVertical == celda.SentidoVertical;
            return esSuPar ? par : null;
        }

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

        public static bool TieneSalidaValida(CeldaGrilla celda)
        {
            if (celda.Tipo == TipoPieza.SALIDA) return true;
            return Sucesores(celda).Count > 0;
        }

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
