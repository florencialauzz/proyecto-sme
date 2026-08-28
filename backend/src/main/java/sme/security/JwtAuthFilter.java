package sme.security;

import io.jsonwebtoken.JwtException;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.List;

// Lee el header "Authorization: Bearer <token>" y, si es válido, autentica el
// request con el usuarioId como principal (decisiones.md: JWT stateless, sin
// sesión del lado del servidor). Los endpoints públicos (/api/auth/**, /api/ping)
// no pasan por acá igual, están excluidos en SecurityConfig.
@Component
public class JwtAuthFilter extends OncePerRequestFilter {

    private final JwtService jwtService;

    public JwtAuthFilter(JwtService jwtService) {
        this.jwtService = jwtService;
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
            throws ServletException, IOException {
        String header = request.getHeader("Authorization");

        if (header != null && header.startsWith("Bearer ")) {
            String token = header.substring("Bearer ".length());
            try {
                Long usuarioId = jwtService.obtenerUsuarioId(token);
                var authentication = new UsernamePasswordAuthenticationToken(usuarioId, null, List.of());
                SecurityContextHolder.getContext().setAuthentication(authentication);
            } catch (JwtException e) {
                // Token inválido o expirado: se deja sin autenticar. SecurityConfig
                // responde 401 al llegar a un endpoint que requiere autenticación.
            }
        }

        filterChain.doFilter(request, response);
    }
}
