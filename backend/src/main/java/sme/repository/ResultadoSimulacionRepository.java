package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import sme.entity.ResultadoSimulacion;

import java.util.Optional;

public interface ResultadoSimulacionRepository extends JpaRepository<ResultadoSimulacion, Long> {
    Optional<ResultadoSimulacion> findByProyectoId(Long proyectoId);

    @Modifying
    @Query("DELETE FROM ResultadoSimulacion r WHERE r.proyectoId = :proyectoId")
    void deleteByProyectoId(@Param("proyectoId") Long proyectoId);
}
