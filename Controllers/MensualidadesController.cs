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

    // ✅ Registrar una nueva mensualidad (con validación de placa vigente)
    [HttpPost]
    public async Task<ActionResult<Mensualidad>> PostMensualidad(Mensualidad mensualidad)
    {
        // 🔹 Verificar si la placa ya tiene una mensualidad vigente (ajuste de zona horaria)
        bool vigente = await _context.mensualidades.AnyAsync(m =>
            m.placa.ToLower() == mensualidad.placa.ToLower() &&
            m.activa &&
            m.fecha_fin.Date >= DateTime.UtcNow.Date);

        if (vigente)
            return Conflict(new { message = "Ya existe una mensualidad vigente para esta placa." });

        // 🔹 Normalizar fechas a UTC antes de guardar (evita error con PostgreSQL)
        if (mensualidad.fecha_inicio.Kind == DateTimeKind.Unspecified)
            mensualidad.fecha_inicio = DateTime.SpecifyKind(mensualidad.fecha_inicio, DateTimeKind.Utc);

        if (mensualidad.fecha_fin.Kind == DateTimeKind.Unspecified)
            mensualidad.fecha_fin = DateTime.SpecifyKind(mensualidad.fecha_fin, DateTimeKind.Utc);

        // 🔹 Guardar la nueva mensualidad
        _context.mensualidades.Add(mensualidad);
        await _context.SaveChangesAsync();

        // 🔹 Enviar correo de confirmación
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

        // 🔹 Normalizar fechas a UTC para evitar errores con PostgreSQL
        if (mensualidad.fecha_inicio.Kind == DateTimeKind.Unspecified)
            mensualidad.fecha_inicio = DateTime.SpecifyKind(mensualidad.fecha_inicio, DateTimeKind.Utc);

        if (mensualidad.fecha_fin.Kind == DateTimeKind.Unspecified)
            mensualidad.fecha_fin = DateTime.SpecifyKind(mensualidad.fecha_fin, DateTimeKind.Utc);

        _context.Entry(mensualidad).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.mensualidades.Any(e => e.id_mensualidad == id))
                return NotFound();
            else
                throw;
        }

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
        // 🔹 Usar UTC para evitar conflictos entre "timestamp with/without time zone"
        var hoy = DateTime.UtcNow.Date;

        // 🔹 Convertir la fecha de la base a DateTime.Date para comparar correctamente
        var proximasAVencer = await _context.mensualidades
            .Where(m => m.activa &&
                        m.fecha_fin.Date <= hoy.AddDays(3) &&
                        m.fecha_fin.Date >= hoy)
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