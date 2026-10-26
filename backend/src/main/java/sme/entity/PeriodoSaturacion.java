package sme.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

@Entity
@Table(name = "periodo_saturacion")
public class PeriodoSaturacion {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "resultado_simulacion_id", nullable = false)
    private Long resultadoSimulacionId;

    // Solo para que Hibernate cree la FK con ON DELETE CASCADE
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "resultado_simulacion_id", insertable = false, updatable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private ResultadoSimulacion resultadoSimulacion;

    @Column(nullable = false)
    private Integer inicio;

    @Column(nullable = false)
    private Integer fin;

    public PeriodoSaturacion() {
    }

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public Long getResultadoSimulacionId() { return resultadoSimulacionId; }
    public void setResultadoSimulacionId(Long resultadoSimulacionId) { this.resultadoSimulacionId = resultadoSimulacionId; }

    public Integer getInicio() { return inicio; }
    public void setInicio(Integer inicio) { this.inicio = inicio; }

    public Integer getFin() { return fin; }
    public void setFin(Integer fin) { this.fin = fin; }
}
