package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import sme.entity.Proyecto;

import java.util.List;

public interface ProyectoRepository extends JpaRepository<Proyecto, Long> {
    boolean existsByUsuarioIdAndNombre(Long usuarioId, String nombre);

    List<Proyecto> findByUsuarioIdOrderByFechaModificacionDesc(Long usuarioId);
}
