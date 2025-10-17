using System.ComponentModel.DataAnnotations;

namespace backend_crudpark.Models;

public class Tarifa
{
    [Key]
    public int id_tarifa { get; set; }
    public string? descripcion { get; set; }
    public decimal valor_base_hora { get; set; }
    public decimal valor_fraccion { get; set; }
    public decimal tope_diario { get; set; }
    public int tiempo_gracia_min { get; set; } = 30;
    public bool activa { get; set; } = true;
}