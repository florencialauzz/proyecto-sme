package sme.dto;

// POST /proyectos/{id}/duplicar: el proyecto nuevo, con el nombre que le
// puso el backend ("<nombre> (copia)").
public record DuplicarProyectoResponse(Long proyectoId, String nombre) {
}
