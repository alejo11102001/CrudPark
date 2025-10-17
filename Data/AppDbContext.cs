using Microsoft.EntityFrameworkCore;
using backend_crudpark.Models;
namespace backend_crudpark.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Operador> operadores { get; set; }
    public DbSet<Tarifa> tarifas { get; set; }
    public DbSet<Mensualidad> mensualidades { get; set; }
    public DbSet<Pago> pagos { get; set; }
    public DbSet<Notificacion> notificaciones { get; set; }
    public DbSet<Ticket> tickets { get; set; }

}