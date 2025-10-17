using backend_crudpark.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_crudpark.Models;
namespace backend_crudpark.Controllers;


[ApiController]
[Route("api/[controller]")]
public class OperadoresController : ControllerBase
{
    private readonly AppDbContext _context;
    public OperadoresController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Operador>>> GetOperadores()
    {
        return await _context.operadores.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Operador>> GetOperador(int id)
    {
        var operador = await _context.operadores.FindAsync(id);
        if (operador == null)
            return NotFound();
        return operador;
    }

    [HttpPost]
    public async Task<ActionResult<Operador>> PostOperador(Operador operador)
    {
        _context.operadores.Add(operador);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOperador), new { id = operador.id_operador }, operador);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOperador(int id, Operador operador)
    {
        if (id != operador.id_operador)
            return BadRequest();

        _context.Entry(operador).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOperador(int id)
    {
        var operador = await _context.operadores.FindAsync(id);
        if (operador == null)
            return NotFound();

        _context.operadores.Remove(operador);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}