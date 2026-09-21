package sme.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "proyecto", uniqueConstraints = {
        @UniqueConstraint(name = "uq_proyecto_usuario_nombre", columnNames = {"usuario_id", "nombre"})
})
public class Proyecto {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "usuario_id", nullable = false)
    private Long usuarioId;

    // Mapea la misma columna que usuarioId, solo de lectura: existe unicamente
    // para que Hibernate genere la foreign key con ON DELETE CASCADE
    // (ver contratos/esquema-bd.md). Las lecturas/escrituras de la relacion
    // siguen haciendose a traves de usuarioId, no de este campo.
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "usuario_id", insertable = false, updatable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private Usuario usuario;

    @Column(nullable = false, length = 100)
    private String nombre;

    // Configuración de simulación (RF-08 a RF-11): se completa recién en Iteración 3.
    @Column(name = "cantidad_pisos")
    private Integer cantidadPisos;

    @Column(name = "frecuencia_ingreso")
    private Integer frecuenciaIngreso;

    @Column(name = "tiempo_permanencia")
    private Integer tiempoPermanencia;

    @Column(name = "filas_grilla", nullable = false)
    private Integer filasGrilla = 15;

    @Column(name = "columnas_grilla", nullable = false)
    private Integer columnasGrilla = 15;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 20)
    private EstadoProyecto estado = EstadoProyecto.FORMULARIO;

    @CreationTimestamp
    @Column(name = "fecha_creacion", nullable = false, updatable = false)
    private LocalDateTime fechaCreacion;

    @UpdateTimestamp
    @Column(name = "fecha_modificacion", nullable = false)
    private LocalDateTime fechaModificacion;

    public Proyecto() {
    }

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public Long getUsuarioId() { return usuarioId; }
    public void setUsuarioId(Long usuarioId) { this.usuarioId = usuarioId; }

    public String getNombre() { return nombre; }
    public void setNombre(String nombre) { this.nombre = nombre; }

    public Integer getCantidadPisos() { return cantidadPisos; }
    public void setCantidadPisos(Integer cantidadPisos) { this.cantidadPisos = cantidadPisos; }

    public Integer getFrecuenciaIngreso() { return frecuenciaIngreso; }
    public void setFrecuenciaIngreso(Integer frecuenciaIngreso) { this.frecuenciaIngreso = frecuenciaIngreso; }

    public Integer getTiempoPermanencia() { return tiempoPermanencia; }
    public void setTiempoPermanencia(Integer tiempoPermanencia) { this.tiempoPermanencia = tiempoPermanencia; }

    public Integer getFilasGrilla() { return filasGrilla; }
    public void setFilasGrilla(Integer filasGrilla) { this.filasGrilla = filasGrilla; }

    public Integer getColumnasGrilla() { return columnasGrilla; }
    public void setColumnasGrilla(Integer columnasGrilla) { this.columnasGrilla = columnasGrilla; }

    public EstadoProyecto getEstado() { return estado; }
    public void setEstado(EstadoProyecto estado) { this.estado = estado; }

    public LocalDateTime getFechaCreacion() { return fechaCreacion; }
    public void setFechaCreacion(LocalDateTime fechaCreacion) { this.fechaCreacion = fechaCreacion; }

    public LocalDateTime getFechaModificacion() { return fechaModificacion; }
    public void setFechaModificacion(LocalDateTime fechaModificacion) { this.fechaModificacion = fechaModificacion; }
}
