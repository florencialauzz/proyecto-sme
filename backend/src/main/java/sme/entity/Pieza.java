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
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

// Espejo de contratos/esquema-bd.md. sentido_vertical se agrega recién con
// Rampa (RF-18) en Iteración 3.
@Entity
@Table(name = "pieza", uniqueConstraints = {
        @UniqueConstraint(name = "uq_pieza_celda", columnNames = {"proyecto_id", "piso", "fila", "columna"})
})
public class Pieza {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "proyecto_id", nullable = false)
    private Long proyectoId;

    // Mapea la misma columna que proyectoId, solo de lectura: existe unicamente
    // para que Hibernate genere la foreign key con ON DELETE CASCADE
    // (ver contratos/esquema-bd.md). Las lecturas/escrituras de la relacion
    // siguen haciendose a traves de proyectoId, no de este campo.
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "proyecto_id", insertable = false, updatable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private Proyecto proyecto;

    @Column(nullable = false)
    private Integer piso;

    @Column(nullable = false)
    private Integer fila;

    @Column(nullable = false)
    private Integer columna;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 20)
    private TipoPieza tipo;

    // Cara de acceso (Plaza, ZonaBicicletasMotos) y dirección (Calle, Entrada,
    // Salida) son columnas separadas por claridad semántica del modelo de
    // dominio (dominio/modelo-clases.md), aunque comparten el mismo enum — el
    // service decide cuál corresponde según el tipo de pieza.
    @Enumerated(EnumType.STRING)
    @Column(name = "cara_acceso", length = 10)
    private Direccion caraAcceso;

    @Enumerated(EnumType.STRING)
    @Column(name = "direccion", length = 10)
    private Direccion direccion;

    @Column(name = "es_accesible")
    private Boolean esAccesible;

    @Column(name = "es_cruce_peatonal")
    private Boolean esCrucePeatonal;

    public Pieza() {
    }

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public Long getProyectoId() { return proyectoId; }
    public void setProyectoId(Long proyectoId) { this.proyectoId = proyectoId; }

    public Integer getPiso() { return piso; }
    public void setPiso(Integer piso) { this.piso = piso; }

    public Integer getFila() { return fila; }
    public void setFila(Integer fila) { this.fila = fila; }

    public Integer getColumna() { return columna; }
    public void setColumna(Integer columna) { this.columna = columna; }

    public TipoPieza getTipo() { return tipo; }
    public void setTipo(TipoPieza tipo) { this.tipo = tipo; }

    public Direccion getCaraAcceso() { return caraAcceso; }
    public void setCaraAcceso(Direccion caraAcceso) { this.caraAcceso = caraAcceso; }

    public Direccion getDireccion() { return direccion; }
    public void setDireccion(Direccion direccion) { this.direccion = direccion; }

    public Boolean getEsAccesible() { return esAccesible; }
    public void setEsAccesible(Boolean esAccesible) { this.esAccesible = esAccesible; }

    public Boolean getEsCrucePeatonal() { return esCrucePeatonal; }
    public void setEsCrucePeatonal(Boolean esCrucePeatonal) { this.esCrucePeatonal = esCrucePeatonal; }
}
