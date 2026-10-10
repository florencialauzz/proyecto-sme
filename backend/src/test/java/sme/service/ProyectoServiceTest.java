package sme.service;

import org.junit.jupiter.api.Test;
import sme.dto.RenombrarProyectoRequest;
import sme.dto.RenombrarProyectoResponse;
import sme.entity.Proyecto;
import sme.exception.NegocioException;
import sme.repository.PiezaRepository;
import sme.repository.ProyectoRepository;

import java.util.Optional;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

// Cambiar el nombre de un proyecto, sin base de datos: los repositorios son
// mocks.
class ProyectoServiceTest {

    private static final long USUARIO_ID = 1L;
    private static final long PROYECTO_ID = 10L;

    private final ProyectoRepository proyectoRepository = mock(ProyectoRepository.class);
    private final ProyectoService proyectoService = new ProyectoService(proyectoRepository, mock(PiezaRepository.class));

    private Proyecto proyectoLlamado(String nombre) {
        Proyecto proyecto = new Proyecto();
        proyecto.setId(PROYECTO_ID);
        proyecto.setUsuarioId(USUARIO_ID);
        proyecto.setNombre(nombre);
        when(proyectoRepository.findById(PROYECTO_ID)).thenReturn(Optional.of(proyecto));
        return proyecto;
    }

    @Test
    void renombrarCambiaElNombre() {
        Proyecto proyecto = proyectoLlamado("Centro");
        when(proyectoRepository.existsByUsuarioIdAndNombre(USUARIO_ID, "Shopping Norte")).thenReturn(false);

        RenombrarProyectoResponse respuesta =
                proyectoService.renombrar(USUARIO_ID, PROYECTO_ID, new RenombrarProyectoRequest("Shopping Norte"));

        assertEquals("Shopping Norte", respuesta.nombre());
        assertEquals("Shopping Norte", proyecto.getNombre());
    }

    @Test
    void renombrarConElMismoNombreNoEsRepetido() {
        proyectoLlamado("Centro");
        when(proyectoRepository.existsByUsuarioIdAndNombre(USUARIO_ID, "Centro")).thenReturn(true);

        RenombrarProyectoResponse respuesta =
                proyectoService.renombrar(USUARIO_ID, PROYECTO_ID, new RenombrarProyectoRequest("Centro"));

        assertEquals("Centro", respuesta.nombre());
    }

    @Test
    void renombrarAUnNombreDeOtroProyectoSeRechaza() {
        proyectoLlamado("Centro");
        when(proyectoRepository.existsByUsuarioIdAndNombre(USUARIO_ID, "Shopping Norte")).thenReturn(true);

        NegocioException error = assertThrows(NegocioException.class,
                () -> proyectoService.renombrar(USUARIO_ID, PROYECTO_ID, new RenombrarProyectoRequest("Shopping Norte")));

        assertEquals("PROYECTO_DUPLICADO", error.getCodigo());
    }

    @Test
    void renombrarConNombreVacioSeRechaza() {
        proyectoLlamado("Centro");

        NegocioException error = assertThrows(NegocioException.class,
                () -> proyectoService.renombrar(USUARIO_ID, PROYECTO_ID, new RenombrarProyectoRequest("  ")));

        assertEquals("NOMBRE_VACIO", error.getCodigo());
    }
}
