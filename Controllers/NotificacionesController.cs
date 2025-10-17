using backend_crudpark.Data;
using backend_crudpark.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
namespace backend_crudpark.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificacionesController: ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public NotificacionesController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // 1️⃣ Listar notificaciones
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notificacion>>> GetNotificaciones()
    {
        return await _context.notificaciones.ToListAsync();
    }

    // 2️⃣ Enviar correo de creación de mensualidad
    [HttpPost("enviar-creacion/{idMensualidad}")]
    public async Task<IActionResult> EnviarCorreoCreacion(int idMensualidad)
    {
        var mensualidad = await _context.mensualidades.FindAsync(idMensualidad);
        if (mensualidad == null || string.IsNullOrEmpty(mensualidad.correo))
            return NotFound("Mensualidad no encontrada o sin correo asociado.");

        // Evitar duplicados
        if (await _context.notificaciones.AnyAsync(n => n.id_mensualidad == idMensualidad && n.tipo == "Creación"))
            return BadRequest("Ya se envió el correo de creación previamente.");

        string asunto = "Registro de mensualidad exitoso";
        string mensaje = $@"
        Hola {mensualidad.nombre_cliente},

        Tu mensualidad ha sido registrada con éxito.
        Fecha de inicio: {mensualidad.fecha_inicio:dd/MM/yyyy}
        Fecha de fin: {mensualidad.fecha_fin:dd/MM/yyyy}

        ¡Gracias por confiar en nuestro servicio!
        ";

        await EnviarCorreoAsync(mensualidad.correo, asunto, mensaje);

        _context.notificaciones.Add(new Notificacion
        {
            id_mensualidad = idMensualidad,
            tipo = "Creación",
            fecha_envio = DateTime.Now,
            enviado = true
        });

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Correo de creación enviado correctamente." });
    }

    // 3️⃣ Enviar correos por mensualidades próximas a vencer
    [HttpPost("enviar-vencimientos")]
    public async Task<IActionResult> EnviarCorreosVencimiento()
    {
        DateTime hoy = DateTime.Today;
        DateTime fechaLimite = hoy.AddDays(3);

        var proximasAVencer = await _context.mensualidades
            .Where(m => m.activa && m.fecha_fin <= fechaLimite && m.fecha_fin >= hoy)
            .ToListAsync();

        if (!proximasAVencer.Any())
            return Ok("No hay mensualidades próximas a vencer.");

        foreach (var mensualidad in proximasAVencer)
        {
            if (string.IsNullOrEmpty(mensualidad.correo)) continue;

            // Evitar duplicados
            if (await _context.notificaciones.AnyAsync(n =>
                n.id_mensualidad == mensualidad.id_mensualidad && n.tipo == "Vencimiento"))
                continue;

            string asunto = "Tu mensualidad está próxima a vencer";
            string mensaje = $@"
            Hola {mensualidad.nombre_cliente},

            Te recordamos que tu mensualidad con placa {mensualidad.placa}
            vence el día {mensualidad.fecha_fin:dd/MM/yyyy}.

            Por favor, realiza la renovación a tiempo para evitar interrupciones.
            ";

            await EnviarCorreoAsync(mensualidad.correo, asunto, mensaje);

            _context.notificaciones.Add(new Notificacion
            {
                id_mensualidad = mensualidad.id_mensualidad,
                tipo = "Vencimiento",
                fecha_envio = DateTime.Now,
                enviado = true
            });
        }

        await _context.SaveChangesAsync();
        return Ok("Correos de vencimiento enviados correctamente.");
    }

    // 🔹 Método auxiliar para envío de correos con System.Net.Mail
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
                From = new MailAddress(_config["Mail:User"], _config["Mail:DisplayName"]),
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