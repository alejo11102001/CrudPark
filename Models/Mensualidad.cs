using System.ComponentModel.DataAnnotations;

namespace backend_crudpark.Models;
public class Mensualidad
{
    [Key]
    public int id_mensualidad { get; set; }
    public string nombre_cliente { get; set; } = string.Empty;
    public string? correo { get; set; }
    public string placa { get; set; } = string.Empty;
    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_fin { get; set; }
    public bool activa { get; set; } = true;
}