using System.ComponentModel.DataAnnotations;

namespace backend_crudpark.Models;

public class Notificacion
{
    [Key]
    public int id_notificacion { get; set; }
    public int? id_mensualidad { get; set; }
    public string tipo { get; set; } = string.Empty;
    public DateTime fecha_envio { get; set; } = DateTime.Now;
    public bool enviado { get; set; } = true;
}