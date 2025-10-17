using System.ComponentModel.DataAnnotations;

namespace backend_crudpark.Models;

public class Operador
{
    [Key]
    public int id_operador { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string? correo { get; set; }
    public bool activo { get; set; } = true;
}