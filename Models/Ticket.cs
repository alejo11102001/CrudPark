using System.ComponentModel.DataAnnotations;

namespace backend_crudpark.Models;

public class Ticket
{
    [Key]
    public int id_ticket { get; set; }
    public string placa { get; set; } = string.Empty;
    public string? tipo { get; set; }
    public DateTime fecha_ingreso { get; set; }
    public DateTime? fecha_salida { get; set; }
    public decimal total_pagar { get; set; }
    public bool pagado { get; set; }
    public int? id_operador { get; set; }
    public string? codigo_qr { get; set; }
}