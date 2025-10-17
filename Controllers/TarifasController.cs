using Microsoft.AspNetCore.Mvc;
using backend_crudpark.Data;
using backend_crudpark.Models;
using Microsoft.EntityFrameworkCore;
namespace backend_crudpark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TarifasController: ControllerBase
{
    private readonly AppDbContext _context;

    public TarifasController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ 1️⃣ Obtener todas las tarifas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tarifa>>> GetTarifas()
    {
        return await _context.tarifas.ToListAsync();
    }

    // ✅ 2️⃣ Obtener una tarifa específica por ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Tarifa>> GetTarifa(int id)
    {
        var tarifa = await _context.tarifas.FindAsync(id);
        if (tarifa == null)
            return NotFound();

        return tarifa;
    }

    // ✅ 3️⃣ Crear una nueva tarifa
    [HttpPost]
    public async Task<ActionResult<Tarifa>> PostTarifa(Tarifa tarifa)
    {
        // Validar tiempo de gracia mínimo de 30 minutos
        if (tarifa.tiempo_gracia_min < 30)
            tarifa.tiempo_gracia_min = 30;

        // Si hay otra tarifa activa, se puede dejar como opcional desactivarla
        var activa = await _context.tarifas.FirstOrDefaultAsync(t => t.activa);
        if (activa != null && tarifa.activa)
        {
            activa.activa = false;
            _context.Entry(activa).State = EntityState.Modified;
        }

        _context.tarifas.Add(tarifa);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTarifa), new { id = tarifa.id_tarifa }, tarifa);
    }

    // ✅ 4️⃣ Actualizar una tarifa existente
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTarifa(int id, Tarifa tarifa)
    {
        if (id != tarifa.id_tarifa)
            return BadRequest();

        if (tarifa.tiempo_gracia_min < 30)
            tarifa.tiempo_gracia_min = 30;

        _context.Entry(tarifa).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ✅ 5️⃣ Desactivar (o eliminar lógicamente) una tarifa
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTarifa(int id)
    {
        var tarifa = await _context.tarifas.FindAsync(id);
        if (tarifa == null)
            return NotFound();

        tarifa.activa = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ✅ 6️⃣ Obtener la tarifa activa actual
    [HttpGet("activa")]
    public async Task<ActionResult<Tarifa>> GetTarifaActiva()
    {
        var activa = await _context.tarifas.FirstOrDefaultAsync(t => t.activa);
        if (activa == null)
            return NotFound(new { message = "No hay una tarifa activa actualmente." });

        return activa;
    }
}