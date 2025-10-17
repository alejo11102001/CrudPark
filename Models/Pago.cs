using System.ComponentModel.DataAnnotations;

namespace backend_crudpark.Models;

public class Pago
{
    [Key]
    public int id_pago { get; set; }
    public int? id_ticket { get; set; }
    public string metodo_pago { get; set; } = string.Empty;
    public decimal monto { get; set; }
    public DateTime fecha_pago { get; set; } = DateTime.Now;
}