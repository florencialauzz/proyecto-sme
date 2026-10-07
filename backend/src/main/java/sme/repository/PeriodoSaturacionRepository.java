package sme.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import sme.entity.PeriodoSaturacion;

import java.util.List;

public interface PeriodoSaturacionRepository extends JpaRepository<PeriodoSaturacion, Long> {

    List<PeriodoSaturacion> findByResultadoSimulacionIdOrderByInicioAsc(Long resultadoSimulacionId);
}
