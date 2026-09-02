package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import sme.dto.CambiarContrasenaRequest;
import sme.dto.CambiarContrasenaResponse;
import sme.dto.EliminarCuentaRequest;
import sme.dto.EliminarCuentaResponse;
import sme.dto.LoginRequest;
import sme.dto.LoginResponse;
import sme.dto.PreguntaSeguridadResponse;
import sme.dto.RecuperarContrasenaRequest;
import sme.dto.RecuperarContrasenaResponse;
import sme.dto.RegistroRequest;
import sme.dto.RegistroResponse;
import sme.entity.Usuario;
import sme.exception.NegocioException;
import sme.repository.UsuarioRepository;
import sme.security.JwtService;

@Service
public class AuthService {

    private final UsuarioRepository usuarioRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtService jwtService;

    public AuthService(UsuarioRepository usuarioRepository, PasswordEncoder passwordEncoder, JwtService jwtService) {
        this.usuarioRepository = usuarioRepository;
        this.passwordEncoder = passwordEncoder;
        this.jwtService = jwtService;
    }

    // RF-01: valida duplicado (A1) y coincidencia de contraseñas (A2) antes de registrar.
    public RegistroResponse registrar(RegistroRequest request) {
        if (usuarioRepository.existsByNombreUsuario(request.nombreUsuario())) {
            throw new NegocioException(HttpStatus.CONFLICT, "USUARIO_DUPLICADO", "Nombre de usuario ya registrado");
        }
        if (!request.contrasena().equals(request.confirmacionContrasena())) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CONTRASENA_NO_COINCIDE",
                    "La contraseña y su confirmación no coinciden");
        }

        Usuario usuario = new Usuario();
        usuario.setNombreUsuario(request.nombreUsuario());
        usuario.setContrasenaHash(passwordEncoder.encode(request.contrasena()));
        usuario.setPreguntaSeguridad(request.preguntaSeguridad());
        usuario.setRespuestaSeguridadHash(passwordEncoder.encode(request.respuestaSeguridad()));

        usuario = usuarioRepository.save(usuario);
        return new RegistroResponse(usuario.getId());
    }

    // RF-02: distingue usuario inexistente (A1) de contraseña incorrecta (A2), tal
    // como lo pide el caso de uso expandido.
    public LoginResponse iniciarSesion(LoginRequest request) {
        Usuario usuario = usuarioRepository.findByNombreUsuario(request.nombreUsuario())
                .orElseThrow(() -> new NegocioException(HttpStatus.UNAUTHORIZED, "USUARIO_INEXISTENTE",
                        "El nombre de usuario ingresado no es correcto"));

        if (!passwordEncoder.matches(request.contrasena(), usuario.getContrasenaHash())) {
            throw new NegocioException(HttpStatus.UNAUTHORIZED, "CONTRASENA_INCORRECTA",
                    "La contraseña ingresada no es correcta");
        }

        String token = jwtService.generarToken(usuario.getId(), usuario.getNombreUsuario());
        return new LoginResponse(token, usuario.getId());
    }

    // RF-03, punto 4-5: valida que el usuario exista (A1) y devuelve su pregunta
    // de seguridad para mostrarla antes de pedir la respuesta.
    public PreguntaSeguridadResponse obtenerPreguntaSeguridad(String nombreUsuario) {
        Usuario usuario = buscarPorNombreUsuarioOFallar(nombreUsuario);
        return new PreguntaSeguridadResponse(usuario.getPreguntaSeguridad());
    }

    // RF-03: valida respuesta de seguridad (A2) y coincidencia de la nueva
    // contraseña con su confirmación (A3) antes de actualizar.
    public RecuperarContrasenaResponse recuperarContrasena(RecuperarContrasenaRequest request) {
        Usuario usuario = buscarPorNombreUsuarioOFallar(request.nombreUsuario());

        if (!passwordEncoder.matches(request.respuestaSeguridad(), usuario.getRespuestaSeguridadHash())) {
            throw new NegocioException(HttpStatus.UNAUTHORIZED, "RESPUESTA_SEGURIDAD_INCORRECTA",
                    "La respuesta ingresada no es correcta");
        }
        if (!request.nuevaContrasena().equals(request.confirmacionNuevaContrasena())) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CONTRASENA_NO_COINCIDE",
                    "La contraseña y su confirmación no coinciden");
        }

        usuario.setContrasenaHash(passwordEncoder.encode(request.nuevaContrasena()));
        usuarioRepository.save(usuario);
        return new RecuperarContrasenaResponse(true);
    }

    // RF-04: valida contraseña actual (A1) y coincidencia de la nueva contraseña
    // con su confirmación (A2) antes de actualizar.
    public CambiarContrasenaResponse cambiarContrasena(Long usuarioId, CambiarContrasenaRequest request) {
        Usuario usuario = buscarPorIdOFallar(usuarioId);

        if (!passwordEncoder.matches(request.contrasenaActual(), usuario.getContrasenaHash())) {
            throw new NegocioException(HttpStatus.UNAUTHORIZED, "CONTRASENA_ACTUAL_INCORRECTA",
                    "La contraseña actual ingresada no es correcta");
        }
        if (!request.contrasenaNueva().equals(request.confirmacionContrasenaNueva())) {
            throw new NegocioException(HttpStatus.BAD_REQUEST, "CONTRASENA_NO_COINCIDE",
                    "La contraseña y su confirmación no coinciden");
        }

        usuario.setContrasenaHash(passwordEncoder.encode(request.contrasenaNueva()));
        usuarioRepository.save(usuario);
        return new CambiarContrasenaResponse(true);
    }

    // RF-05: valida contraseña (A1) antes de eliminar la cuenta. El borrado en
    // cascada de proyectos/piezas/resultados lo resuelve la BD (esquema-bd.md).
    public EliminarCuentaResponse eliminarCuenta(Long usuarioId, EliminarCuentaRequest request) {
        Usuario usuario = buscarPorIdOFallar(usuarioId);

        if (!passwordEncoder.matches(request.contrasena(), usuario.getContrasenaHash())) {
            throw new NegocioException(HttpStatus.UNAUTHORIZED, "CONTRASENA_INCORRECTA",
                    "La contraseña ingresada no es correcta");
        }

        usuarioRepository.delete(usuario);
        return new EliminarCuentaResponse(true);
    }

    private Usuario buscarPorNombreUsuarioOFallar(String nombreUsuario) {
        return usuarioRepository.findByNombreUsuario(nombreUsuario)
                .orElseThrow(() -> new NegocioException(HttpStatus.NOT_FOUND, "USUARIO_INEXISTENTE",
                        "El nombre de usuario ingresado no existe"));
    }

    private Usuario buscarPorIdOFallar(Long usuarioId) {
        return usuarioRepository.findById(usuarioId)
                .orElseThrow(() -> new NegocioException(HttpStatus.NOT_FOUND, "USUARIO_INEXISTENTE",
                        "Usuario no encontrado"));
    }
}
