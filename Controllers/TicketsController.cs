using Microsoft.AspNetCore.Mvc;
using backend_crudpark.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using OfficeOpenXml; // EPPlus para exportar Excel
using backend_crudpark.Models;
namespace backend_crudpark.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TicketsController: ControllerBase
{
    private readonly AppDbContext _context;
    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // 1️⃣ Ingresos diarios, semanales y mensuales
    // ==========================================
    [HttpGet("ingresos")]
    public async Task<IActionResult> GetIngresos()
    {
        // 1️⃣ Traer todos los tickets relevantes a memoria (C#) para evitar problemas de traducción de EF Core
        var tickets = await _context.tickets
            .Where(t => t.fecha_salida != null && t.total_pagar > 0)
            .ToListAsync();

        // 2️⃣ Definir la fecha actual en UTC
        DateTime hoy = DateTime.UtcNow.Date;

        // 3️⃣ Agrupar por día, semana y mes en memoria
        var ingresos = tickets
            .GroupBy(t => new
            {
                Dia = t.fecha_salida.Value.ToUniversalTime().Date,
                Semana = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    t.fecha_salida.Value.ToUniversalTime(),
                    CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday
                ),
                Mes = t.fecha_salida.Value.ToUniversalTime().Month
            })
            .Select(g => new
            {
                Fecha = g.Key.Dia.ToString("yyyy-MM-dd"),
                Semana = g.Key.Semana,
                Mes = g.Key.Mes,
                Total = g.Sum(t => t.total_pagar)
            })
            .OrderBy(g => g.Fecha)
            .ToList();

        // 4️⃣ Retornar resultado
        return Ok(ingresos);
    }


    // ==========================================
    // 2️⃣ Promedio de ocupación
    // ==========================================
    [HttpGet("ocupacion")]
    public async Task<IActionResult> GetPromedioOcupacion()
    {
        // Total de vehículos actualmente dentro del parqueadero
        var dentro = await _context.tickets
            .Where(t => t.fecha_salida == null)
            .CountAsync();

        // Suponiendo un total de 100 espacios disponibles (puedes ajustarlo)
        int capacidadTotal = 100;

        double promedio = (double)dentro / capacidadTotal * 100;

        return Ok(new
        {
            Dentro = dentro,
            CapacidadTotal = capacidadTotal,
            PorcentajeOcupacion = promedio
        });
    }

    // ==========================================
    // 3️⃣ Comparativa: mensualidades vs invitados
    // ==========================================
    [HttpGet("comparativa")]
    public async Task<IActionResult> GetComparativa()
    {
        var mensualidades = await _context.tickets
            .Where(t => t.tipo.ToLower() == "mensual")
            .CountAsync();

        var invitados = await _context.tickets
            .Where(t => t.tipo.ToLower() == "invitado")
            .CountAsync();

        return Ok(new
        {
            Mensualidades = mensualidades,
            Invitados = invitados
        });
    }

    // ==========================================
    // 4️⃣ Exportar datos a CSV
    // ==========================================
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportarCSV()
    {
        var tickets = await _context.tickets
            .Select(t => new
            {
                t.id_ticket,
                t.placa,
                t.tipo,
                t.fecha_ingreso,
                t.fecha_salida,
                t.total_pagar,
                t.pagado
            })
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("ID,Placa,Tipo,FechaIngreso,FechaSalida,TotalPagar,Pagado");

        foreach (var t in tickets)
        {
            csv.AppendLine($"{t.id_ticket},{t.placa},{t.tipo},{t.fecha_ingreso},{t.fecha_salida},{t.total_pagar},{t.pagado}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "ReporteTickets.csv");
    }

    // ==========================================
    // 5️⃣ Exportar datos a Excel (EPPlus)
    // ==========================================
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportarExcel()
    {
        var tickets = await _context.tickets
            .Select(t => new
            {
                t.id_ticket,
                t.placa,
                t.tipo,
                t.fecha_ingreso,
                t.fecha_salida,
                t.total_pagar,
                t.pagado
            })
            .ToListAsync();

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Tickets");

        // Encabezados
        ws.Cells["A1"].Value = "ID";
        ws.Cells["B1"].Value = "Placa";
        ws.Cells["C1"].Value = "Tipo";
        ws.Cells["D1"].Value = "Fecha Ingreso";
        ws.Cells["E1"].Value = "Fecha Salida";
        ws.Cells["F1"].Value = "Total Pagar";
        ws.Cells["G1"].Value = "Pagado";

        int fila = 2;
        foreach (var t in tickets)
        {
            ws.Cells[fila, 1].Value = t.id_ticket;
            ws.Cells[fila, 2].Value = t.placa;
            ws.Cells[fila, 3].Value = t.tipo;
            ws.Cells[fila, 4].Value = t.fecha_ingreso.ToString("dd/MM/yyyy HH:mm");
            ws.Cells[fila, 5].Value = t.fecha_salida?.ToString("dd/MM/yyyy HH:mm");
            ws.Cells[fila, 6].Value = (double)t.total_pagar;
            ws.Cells[fila, 7].Value = t.pagado ? "Sí" : "No";
            fila++;
        }

        ws.Cells["A:G"].AutoFitColumns();

        var stream = new MemoryStream(package.GetAsByteArray());
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteTickets.xlsx");
    }
}