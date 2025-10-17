using Microsoft.AspNetCore.Mvc;
using backend_crudpark.Data;
using backend_crudpark.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
namespace backend_crudpark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MensualidadesController: ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config; // para usar credenciales del correo

    public MensualidadesController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // ✅ 1️⃣ Obtener todas las mensualidades
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Mensualidad>>> GetMensualidades()
    {
        return await _context.mensualidades.ToListAsync();
    }

    // ✅ 2️⃣ Obtener mensualidad por ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Mensualidad>> GetMensualidad(int id)
    {
        var mensualidad = await _context.mensualidades.FindAsync(id);
        if (mensualidad == null)
            return NotFound();

        return mensualidad;
    }

    // ✅ 3️⃣ Registrar una nueva mensualidad (con validación de placa vigente)
    [HttpPost]
    public async Task<ActionResult<Mensualidad>> PostMensualidad(Mensualidad mensualidad)
    {
        // Verificar si la placa ya tiene una mensualidad vigente
        bool vigente = await _context.mensualidades.AnyAsync(m =>
            m.placa.ToLower() == mensualidad.placa.ToLower() &&
            m.activa &&
            m.fecha_fin >= DateTime.Today);

        if (vigente)
            return Conflict(new { message = "Ya existe una mensualidad vigente para esta placa." });

        _context.mensualidades.Add(mensualidad);
        await _context.SaveChangesAsync();

        // Enviar correo de confirmación (opcional)
        if (!string.IsNullOrEmpty(mensualidad.correo))
        {
            await EnviarCorreoAsync(
                mensualidad.correo,
                "Registro de Mensualidad",
                $"Hola {mensualidad.nombre_cliente}, tu mensualidad fue registrada con éxito.\n" +
                $"Placa: {mensualidad.placa}\nInicio: {mensualidad.fecha_inicio:d}\nFin: {mensualidad.fecha_fin:d}"
            );
        }

        return CreatedAtAction(nameof(GetMensualidad), new { id = mensualidad.id_mensualidad }, mensualidad);
    }

    // ✅ 4️⃣ Actualizar una mensualidad existente
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMensualidad(int id, Mensualidad mensualidad)
    {
        if (id != mensualidad.id_mensualidad)
            return BadRequest();

        _context.Entry(mensualidad).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ✅ 5️⃣ Eliminar (o desactivar) mensualidad
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMensualidad(int id)
    {
        var mensualidad = await _context.mensualidades.FindAsync(id);
        if (mensualidad == null)
            return NotFound();

        mensualidad.activa = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ✅ 6️⃣ Enviar correos automáticos a mensualidades próximas a vencer (3 días)
    [HttpPost("enviar-recordatorios")]
    public async Task<IActionResult> EnviarRecordatorios()
    {
        var hoy = DateTime.Today;
        var proximasAVencer = await _context.mensualidades
            .Where(m => m.activa && m.fecha_fin <= hoy.AddDays(3) && m.fecha_fin >= hoy)
            .ToListAsync();

        foreach (var m in proximasAVencer)
        {
            if (!string.IsNullOrEmpty(m.correo))
            {
                await EnviarCorreoAsync(
                    m.correo,
                    "Recordatorio de vencimiento",
                    $"Hola {m.nombre_cliente}, tu mensualidad está próxima a vencer el {m.fecha_fin:d}. " +
                    $"Por favor realiza la renovación a tiempo para evitar inconvenientes."
                );
            }
        }

        return Ok(new { cantidad = proximasAVencer.Count });
    }

    // 🔹 Método auxiliar para envío de correos
    private async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo)
    {
        try
        {
            var smtpClient = new SmtpClient(_config["Mail:Smtp"], int.Parse(_config["Mail:Port"]))
            {
                Credentials = new NetworkCredential(_config["Mail:User"], _config["Mail:Password"]),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_config["Mail:User"]),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false
            };
            mail.To.Add(destinatario);

            await smtpClient.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar correo: {ex.Message}");
        }
    }
}