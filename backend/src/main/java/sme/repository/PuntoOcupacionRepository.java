package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import sme.entity.PuntoOcupacion;

import java.util.List;

public interface PuntoOcupacionRepository extends JpaRepository<PuntoOcupacion, Long> {

    List<PuntoOcupacion> findByResultadoSimulacionIdOrderByMinutoAsc(Long resultadoSimulacionId);
}
