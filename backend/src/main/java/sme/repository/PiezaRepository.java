package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import sme.entity.Pieza;

import java.util.List;

public interface PiezaRepository extends JpaRepository<Pieza, Long> {

    void deleteByProyectoId(Long proyectoId);

    List<Pieza> findByProyectoId(Long proyectoId);
}
