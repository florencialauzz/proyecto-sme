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

// RF-27 guardado (RF-23): un punto de la curva de ocupación. Espejo de
// contratos/esquema-bd.md.
@Entity
@Table(name = "punto_ocupacion")
public class PuntoOcupacion {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "resultado_simulacion_id", nullable = false)
    private Long resultadoSimulacionId;

    // Solo de lectura, para la foreign key con ON DELETE CASCADE (mismo
    // criterio que Pieza.proyecto).
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "resultado_simulacion_id", insertable = false, updatable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private ResultadoSimulacion resultadoSimulacion;

    @Column(nullable = false)
    private Integer minuto;

    @Column(name = "cantidad_ocupadas", nullable = false)
    private Integer cantidadOcupadas;

    public PuntoOcupacion() {
    }

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public Long getResultadoSimulacionId() { return resultadoSimulacionId; }
    public void setResultadoSimulacionId(Long resultadoSimulacionId) { this.resultadoSimulacionId = resultadoSimulacionId; }

    public Integer getMinuto() { return minuto; }
    public void setMinuto(Integer minuto) { this.minuto = minuto; }

    public Integer getCantidadOcupadas() { return cantidadOcupadas; }
    public void setCantidadOcupadas(Integer cantidadOcupadas) { this.cantidadOcupadas = cantidadOcupadas; }
}
