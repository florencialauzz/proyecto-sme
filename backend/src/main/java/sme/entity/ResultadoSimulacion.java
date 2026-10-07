package sme.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.OneToOne;
import jakarta.persistence.Table;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

import java.math.BigDecimal;
import java.time.LocalDateTime;

// RF-23: los resultados guardados de una simulación. Espejo de
// contratos/esquema-bd.md. Un solo resultado por proyecto: guardar otro
// reemplaza al anterior (proyecto_id es UNIQUE). La curva y los períodos de
// saturación van en sus propias tablas (PuntoOcupacion, PeriodoSaturacion).
@Entity
@Table(name = "resultado_simulacion")
public class ResultadoSimulacion {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "proyecto_id", nullable = false, unique = true)
    private Long proyectoId;

    // Mapea la misma columna que proyectoId, solo de lectura: existe unicamente
    // para que Hibernate genere la foreign key con ON DELETE CASCADE (mismo
    // criterio que Pieza.proyecto).
    @OneToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "proyecto_id", insertable = false, updatable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private Proyecto proyecto;

    @Column(name = "eficiencia_espacial", nullable = false, precision = 5, scale = 2)
    private BigDecimal eficienciaEspacial;

    @Column(name = "calificacion_eficiencia", nullable = false, length = 20)
    private String calificacionEficiencia;

    @Column(name = "demanda_satisfecha", nullable = false, precision = 5, scale = 2)
    private BigDecimal demandaSatisfecha;

    @Column(name = "vehiculos_rechazados", nullable = false)
    private Integer vehiculosRechazados;

    @Column(name = "puntuacion_general", nullable = false, precision = 5, scale = 2)
    private BigDecimal puntuacionGeneral;

    @Column(name = "calificacion_texto", nullable = false, length = 20)
    private String calificacionTexto;

    @CreationTimestamp
    @Column(name = "fecha_simulacion", nullable = false, updatable = false)
    private LocalDateTime fechaSimulacion;

    public ResultadoSimulacion() {
    }

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public Long getProyectoId() { return proyectoId; }
    public void setProyectoId(Long proyectoId) { this.proyectoId = proyectoId; }

    public BigDecimal getEficienciaEspacial() { return eficienciaEspacial; }
    public void setEficienciaEspacial(BigDecimal eficienciaEspacial) { this.eficienciaEspacial = eficienciaEspacial; }

    public String getCalificacionEficiencia() { return calificacionEficiencia; }
    public void setCalificacionEficiencia(String calificacionEficiencia) { this.calificacionEficiencia = calificacionEficiencia; }

    public BigDecimal getDemandaSatisfecha() { return demandaSatisfecha; }
    public void setDemandaSatisfecha(BigDecimal demandaSatisfecha) { this.demandaSatisfecha = demandaSatisfecha; }

    public Integer getVehiculosRechazados() { return vehiculosRechazados; }
    public void setVehiculosRechazados(Integer vehiculosRechazados) { this.vehiculosRechazados = vehiculosRechazados; }

    public BigDecimal getPuntuacionGeneral() { return puntuacionGeneral; }
    public void setPuntuacionGeneral(BigDecimal puntuacionGeneral) { this.puntuacionGeneral = puntuacionGeneral; }

    public String getCalificacionTexto() { return calificacionTexto; }
    public void setCalificacionTexto(String calificacionTexto) { this.calificacionTexto = calificacionTexto; }

    public LocalDateTime getFechaSimulacion() { return fechaSimulacion; }
    public void setFechaSimulacion(LocalDateTime fechaSimulacion) { this.fechaSimulacion = fechaSimulacion; }
}
