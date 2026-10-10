package sme.service;

import org.junit.jupiter.api.Test;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import sme.dto.CambiarContrasenaRequest;
import sme.dto.RegistroRequest;
import sme.entity.Usuario;
import sme.exception.NegocioException;
import sme.repository.UsuarioRepository;
import sme.security.JwtService;

import java.util.Optional;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

// Campos obligatorios del registro (RF-01) y de la contraseña nueva (RF-04),
// sin base de datos: el repositorio es un mock.
class AuthServiceTest {

    private final UsuarioRepository usuarioRepository = mock(UsuarioRepository.class);
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();
    private final AuthService authService = new AuthService(usuarioRepository, passwordEncoder, mock(JwtService.class));

    @Test
    void registroConSoloElNombreDeUsuarioSeRechaza() {
        RegistroRequest request = new RegistroRequest("lucag", "", "", "¿Nombre de tu primera mascota?", "");

        NegocioException error = assertThrows(NegocioException.class, () -> authService.registrar(request));

        assertEquals("CAMPOS_INCOMPLETOS", error.getCodigo());
        verify(usuarioRepository, never()).save(any());
    }

    @Test
    void registroConUnCampoDeSoloEspaciosSeRechaza() {
        RegistroRequest request = new RegistroRequest("lucag", "clave", "clave", "¿Nombre de tu primera mascota?", "   ");

        NegocioException error = assertThrows(NegocioException.class, () -> authService.registrar(request));

        assertEquals("CAMPOS_INCOMPLETOS", error.getCodigo());
    }

    @Test
    void registroCompletoSeGuarda() {
        RegistroRequest request = new RegistroRequest("lucag", "clave", "clave", "¿Nombre de tu primera mascota?", "Toby");
        when(usuarioRepository.existsByNombreUsuario("lucag")).thenReturn(false);
        when(usuarioRepository.save(any(Usuario.class))).thenAnswer(invocacion -> {
            Usuario usuario = invocacion.getArgument(0);
            usuario.setId(1L);
            return usuario;
        });

        assertEquals(1L, authService.registrar(request).usuarioId());
    }

    @Test
    void cambiarAUnaContrasenaVaciaSeRechaza() {
        Usuario usuario = new Usuario();
        usuario.setId(1L);
        usuario.setContrasenaHash(passwordEncoder.encode("actual"));
        when(usuarioRepository.findById(1L)).thenReturn(Optional.of(usuario));

        NegocioException error = assertThrows(NegocioException.class,
                () -> authService.cambiarContrasena(1L, new CambiarContrasenaRequest("actual", "", "")));

        assertEquals("CAMPOS_INCOMPLETOS", error.getCodigo());
    }
}
