package sme.service;

import org.springframework.http.HttpStatus;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import sme.dto.LoginRequest;
import sme.dto.LoginResponse;
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
}
