package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import sme.entity.Pieza;

public interface PiezaRepository extends JpaRepository<Pieza, Long> {

    void deleteByProyectoId(Long proyectoId);
}
