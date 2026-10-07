package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import sme.entity.ResultadoSimulacion;

import java.util.Optional;

public interface ResultadoSimulacionRepository extends JpaRepository<ResultadoSimulacion, Long> {

    Optional<ResultadoSimulacion> findByProyectoId(Long proyectoId);

    // Borrado directo en la base (no carga la entidad): así los puntos de la
    // curva y los períodos de saturación los borra el ON DELETE CASCADE, sin
    // tener que borrarlos uno por uno antes.
    @Modifying
    @Query("DELETE FROM ResultadoSimulacion r WHERE r.proyectoId = :proyectoId")
    void deleteByProyectoId(@Param("proyectoId") Long proyectoId);
}
