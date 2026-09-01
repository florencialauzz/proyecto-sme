using System.Collections;
using System.Collections.Generic;
using Sme.Grid;
using Sme.Managers;
using Sme.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sme.UI
{
    // Controlador de la escena Editor: mensajes compartidos (RF-13, ver
    // MensajesEditor) y guardar proyecto (RF-21).
    public class EditorScreen : MonoBehaviour
    {
        private const float DuracionMensajeSegundos = 2.5f;

        [SerializeField] private TMP_Text textoMensaje;
        [SerializeField] private Button botonGuardar;
        [SerializeField] private Button botonSalir;

        [SerializeField] private UnityEvent alSalir;

        private Coroutine ocultamientoEnCurso;

        private void Awake()
        {
            MensajesEditor.Registrar(this);
            textoMensaje.gameObject.SetActive(false);
            botonGuardar.onClick.AddListener(GuardarProyecto);
            botonSalir.onClick.AddListener(Salir);
        }

        public void MostrarMensaje(string mensaje)
        {
            textoMensaje.text = mensaje;
            textoMensaje.gameObject.SetActive(true);

            if (ocultamientoEnCurso != null)
            {
                StopCoroutine(ocultamientoEnCurso);
            }

            ocultamientoEnCurso = StartCoroutine(OcultarLuegoDe(DuracionMensajeSegundos));
        }

        private IEnumerator OcultarLuegoDe(float segundos)
        {
            yield return new WaitForSeconds(segundos);
            textoMensaje.gameObject.SetActive(false);
            ocultamientoEnCurso = null;
        }

        private void GuardarProyecto()
        {
            botonGuardar.interactable = false;

            var request = new GuardarGrillaRequest
            {
                piezas = RecolectarPiezas()
            };

            ApiClient.Put<GuardarGrillaRequest, GuardarGrillaResponse>(
                $"/proyectos/{ProyectoManager.ProyectoId}/grilla",
                request,
                alTenerExito: _ =>
                {
                    botonGuardar.interactable = true;
                    MostrarMensaje("Proyecto guardado.");
                },
                alFallar: (mensaje, codigo) =>
                {
                    botonGuardar.interactable = true;
                    MostrarMensaje(mensaje);
                });
        }

        // El botón no guarda solo: si hay cambios sin guardar, es el usuario
        // quien decide si vuelve a Inicio de todos modos.
        private void Salir()
        {
            alSalir?.Invoke();
        }

        // Recorre toda la grilla y arma una fila por cada Plaza colocada, en su
        // celda ancla (dominio/modelo-clases.md) — la segunda celda no se
        // guarda aparte, el backend la vuelve a inferir de caraAcceso.
        private PiezaDto[] RecolectarPiezas()
        {
            var piezas = new List<PiezaDto>();

            foreach (CeldaView celda in GrillaGenerador.ObtenerTodasLasCeldas())
            {
                PiezaView pieza = celda.GetComponentInChildren<PiezaView>();
                if (pieza == null) continue;

                piezas.Add(new PiezaDto
                {
                    piso = 0,
                    fila = celda.Fila,
                    columna = celda.Columna,
                    tipo = "PLAZA",
                    caraAcceso = pieza.CaraAcceso.ToString(),
                    esAccesible = false
                });
            }

            return piezas.ToArray();
        }
    }
}
