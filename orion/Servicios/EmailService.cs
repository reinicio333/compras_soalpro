using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace orion.Servicios
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarAsync(List<(string Email, string Nombre)> destinatarios, string asunto, string cuerpoHtml)
        {
            if (destinatarios == null || destinatarios.Count == 0)
            {
                return;
            }

            var settings = _configuration.GetSection("EmailSettings");
            var host = settings["Host"];
            var port = int.TryParse(settings["Port"], out var parsedPort) ? parsedPort : 587;
            var useSsl = bool.TryParse(settings["UseSsl"], out var parsedUseSsl) && parsedUseSsl;
            var username = settings["Username"];
            var password = settings["Password"];

            var cuerpoConPlantilla = ConstruirHtml(asunto, cuerpoHtml);

            foreach (var (email, nombre) in destinatarios)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                try
                {
                    var mensaje = new MimeMessage();
                    mensaje.From.Add(new MailboxAddress("Compras Soalpro", "compras@soalpro.com"));
                    mensaje.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(nombre) ? email : nombre, email));
                    mensaje.Subject = asunto;
                    mensaje.Body = new BodyBuilder
                    {
                        HtmlBody = cuerpoConPlantilla
                    }.ToMessageBody();

                    using var smtp = new SmtpClient();
                    var secureSocketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                    await smtp.ConnectAsync(host, port, secureSocketOptions);

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        await smtp.AuthenticateAsync(username, password);
                    }

                    await smtp.SendAsync(mensaje);
                    await smtp.DisconnectAsync(true);
                }
                catch
                {
                    // Omitir errores por destinatario para no cortar el proceso.
                }
            }
        }

        private static string ConstruirHtml(string asunto, string cuerpoHtml)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>{asunto}</title>
</head>
<body style='margin:0;padding:20px;background:#ffffff;font-family:Arial,sans-serif;color:#111827;'>
    <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='max-width:700px;margin:0 auto;border-collapse:collapse;'>
        <tr>
            <td style='background:#1e40af;color:#ffffff;padding:16px 20px;font-size:22px;font-weight:bold;'>
                Sistema Orion
            </td>
        </tr>
        <tr>
            <td style='border:1px solid #e5e7eb;padding:20px;background:#ffffff;'>
                {cuerpoHtml}
            </td>
        </tr>
        <tr>
            <td style='background:#f3f4f6;color:#6b7280;padding:14px 20px;font-size:12px;'>
                compras@soalpro.com
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
