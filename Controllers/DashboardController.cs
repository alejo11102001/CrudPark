using backend_crudpark.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace backend_crudpark.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController: ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        DateTime proximos3dias = hoy.AddDays(3);

        // ==============================
        // Vehículos actualmente dentro
        // ==============================
        var vehiculosDentro = await _context.tickets
            .Where(t => t.fecha_salida == null)
            .CountAsync();

        // ==============================
        // Ingresos del día
        // ==============================
        var ingresosDia = await _context.tickets
            .Where(t => t.fecha_salida != null && t.fecha_salida.Value.Date == hoy)
            .ToListAsync();

        decimal ingresosTotales = ingresosDia.Sum(t => t.total_pagar);

        var ingresosPorTipo = ingresosDia
            .GroupBy(t => t.tipo) // Mensualidad / Invitado
            .Select(g => new { tipo = g.Key, cantidad = g.Count() })
            .ToList();

        // ==============================
        // Ingresos semanales (SOLUCIÓN UNIVERSAL)
        // ==============================
        // 1. Traemos los datos necesarios de la BD
        var todosLosIngresos = await _context.tickets // Comienza el query
            .Where(t => t.fecha_salida != null && t.total_pagar > 0)
            .Select(t => new { t.fecha_salida, t.total_pagar }) // Selecciona solo los datos necesarios
            .ToListAsync(); // <-- ¡AQUÍ ES DONDE CAMBIAS EL CONTEXTO!
        // Esta llamada ejecuta el SQL y trae los datos a C# (memoria).

// 2. Ejecución en el Servidor de la Aplicación (C#)
        var ingresosSemana = todosLosIngresos // Ahora ya es una lista de C# en memoria.
            .GroupBy(t => new
            {
                Año = t.fecha_salida.Value.Year,
                // La función GetWeekOfYear funciona aquí porque no se está traduciendo a SQL.
                Semana = System.Globalization.ISOWeek.GetWeekOfYear(t.fecha_salida.Value)
            })
            .Select(g => new
            {
                g.Key.Año,
                g.Key.Semana,
                Total = g.Sum(t => t.total_pagar)
            })
            .ToList();

        // ==============================
        // Ingresos mensuales
        // ==============================
        var ingresosMes = await _context.tickets
            .Where(t => t.fecha_salida != null && t.total_pagar > 0)
            .GroupBy(t => new
            {
                Año = t.fecha_salida.Value.Year,
                Mes = t.fecha_salida.Value.Month
            })
            .Select(g => new
            {
                g.Key.Año,
                g.Key.Mes,
                Total = g.Sum(t => t.total_pagar)
            })
            .ToListAsync();

        // ==============================
        // Mensualidades
        // ==============================
        var mensualidadesActivas = await _context.mensualidades
            .Where(m => m.activa && m.fecha_fin.Date >= hoy)
            .CountAsync();

        var proximasAVencer = await _context.mensualidades
            .Where(m => m.activa && m.fecha_fin.Date > hoy && m.fecha_fin.Date <= proximos3dias)
            .CountAsync();

        var vencidas = await _context.mensualidades
            .Where(m => !m.activa || m.fecha_fin.Date < hoy)
            .CountAsync();

        // ==============================
        // Respuesta JSON
        // ==============================
        return Ok(new
        {
            vehiculosDentro,
            ingresosTotales,
            ingresosPorTipo,
            ingresosSemana,
            ingresosMes,
            mensualidadesActivas,
            proximasAVencer,
            vencidas
        });
    }
}