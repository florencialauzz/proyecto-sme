package sme.security;

import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.Date;

@Service
public class JwtService {

    // RNF-02: la sesión expira a las 24 horas de iniciada.
    private static final long EXPIRACION_MS = 24 * 60 * 60 * 1000;

    private final SecretKey clave;

    public JwtService(@Value("${sme.jwt.secret}") String secreto) {
        this.clave = Keys.hmacShaKeyFor(secreto.getBytes(StandardCharsets.UTF_8));
    }

    public String generarToken(Long usuarioId, String nombreUsuario) {
        Instant ahora = Instant.now();
        return Jwts.builder()
                .subject(nombreUsuario)
                .claim("usuarioId", usuarioId)
                .issuedAt(Date.from(ahora))
                .expiration(Date.from(ahora.plusMillis(EXPIRACION_MS)))
                .signWith(clave)
                .compact();
    }
}
