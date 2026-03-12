using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Globalization;
using System.Text;

namespace orion.Servicios
{
    public enum TipoNotificacion
    {
        Normal,
        Aprobacion,
        Recepcion,
        Rechazo,
        Actualizacion,
        Eliminado,
        PreAutorizacion
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarAsync(
            List<(string Email, string Nombre)> destinatarios,
            string asunto,
            string cuerpoHtml,
            TipoNotificacion tipo = TipoNotificacion.Normal)
        {
            if (destinatarios == null || destinatarios.Count == 0)
                return;

            var settings = _configuration.GetSection("EmailSettings");
            var host = settings["Host"];
            var port = int.TryParse(settings["Port"], out var p) ? p : 587;
            var useSsl = bool.TryParse(settings["UseSsl"], out var s) && s;
            var fecha = DateTime.Now.ToString("dd/MM/yyyy", new CultureInfo("es-ES"));
            var username = settings["Username"];
            var password = settings["Password"];

            foreach (var (email, nombre) in destinatarios)
            {
                if (string.IsNullOrWhiteSpace(email)) continue;

                try
                {
                    var builder = new BodyBuilder
                    {
                        HtmlBody = ConstruirHtml(asunto, cuerpoHtml, fecha, tipo)
                    };
                    

                    var mensaje = new MimeMessage();
                    mensaje.From.Add(new MailboxAddress("Compras Soalpro", username));
                    mensaje.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(nombre) ? email : nombre, email));
                    mensaje.Subject = asunto;
                    mensaje.Body = builder.ToMessageBody();

                    using var smtp = new SmtpClient();
                    smtp.ServerCertificateValidationCallback = (s2, c, h, e) => true;
                    smtp.Timeout = 30000;

                    await smtp.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        await smtp.AuthenticateAsync(username, password);

                    await smtp.SendAsync(mensaje);
                    await smtp.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando correo a {email}: {ex.Message}");
                }
            }
        }


        private static string ConstruirHtml(string asunto, string cuerpoHtml, string fecha, TipoNotificacion tipo)
        {
            var (accentColor, badgeBg, badgeText, badgeLabel) = tipo switch
            {
                TipoNotificacion.Aprobacion => ("#16a34a", "#dcfce7", "#15803d", "APROBADO"),
                TipoNotificacion.Recepcion => ("#0284c7", "#dbeafe", "#1d4ed8", "RECIBIDO"),
                TipoNotificacion.Rechazo => ("#dc2626", "#fee2e2", "#b91c1c", "RECHAZADO"),
                TipoNotificacion.Actualizacion => ("#ea580c", "#ffedd5", "#c2410c", "ACTUALIZADO"),
                TipoNotificacion.Eliminado => ("#dc2626", "#fee2e2", "#b91c1c", "ELIMINADO"),
                TipoNotificacion.PreAutorizacion => ("#ea580c", "#ffedd5", "#c2410c", "PENDIENTE"),
                _ => ("#2563eb", "#dbeafe", "#1d4ed8", "NUEVO")
            };

            return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
  <title>{asunto}</title>
</head>
<body style=""margin:0;padding:0;background:#f1f5f9;"">

<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background:#f1f5f9;padding:32px 16px;"">
  <tr><td align=""center"">
  <table cellpadding=""0"" cellspacing=""0"" width=""560"" style=""max-width:560px;width:100%;"">

    <!-- barra color top -->
    <tr>
      <td style=""background:{accentColor};height:5px;border-radius:8px 8px 0 0;""></td>
    </tr>

    <!-- HEADER -->
    <tr>
      <td style=""background:#1e293b;padding:24px 32px;"">
        <table cellpadding=""0"" cellspacing=""0"" width=""100%"">
          <tr>
            <td>
              <p style=""margin:0 0 4px;font-family:Helvetica,Arial,sans-serif;font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;color:#64748b;"">SISTEMA ORION</p>
              <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:18px;font-weight:600;color:#f8fafc;line-height:1.3;"">{asunto}</p>
            </td>
            <td align=""right"" valign=""middle"" style=""padding-left:16px;white-space:nowrap;"">
              <span style=""display:inline-block;background:{badgeBg};color:{badgeText};font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;padding:5px 10px;border-radius:4px;"">{badgeLabel}</span>
            </td>
          </tr>
        </table>
      </td>
    </tr>

    <!-- BODY -->
    <tr>
      <td style=""background:#ffffff;padding:32px 32px 24px;"">
        {cuerpoHtml}
      </td>
    </tr>

    <!-- FOOTER -->
    <tr>
      <td style=""background:#f8fafc;padding:16px 32px;border-top:1px solid #e2e8f0;"">
        <table cellpadding=""0"" cellspacing=""0"" width=""100%"">
          <tr>
            <td>
              <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:11px;color:#94a3b8;"">SOALPRO &mdash; 192.168.1.1:86</p>
            </td>
            
          </tr>
          
        </table>
      </td>
    </tr>

    <!-- barra color bottom -->
    <tr>
      <td style=""background:{accentColor};height:3px;border-radius:0 0 8px 8px;""></td>
    </tr>

  </table>
  </td></tr>
</table>

</body>
</html>";
        }

        // ── Helpers de contenido ─────────────────────────────────────────────────

        public static string FilaDato(string etiqueta, string valor)
        {
            return $@"<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-bottom:14px;"">
  <tr>
    <td>
      <p style=""margin:0 0 2px;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:600;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">{etiqueta}</p>
      <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:14px;color:#1e293b;"">{valor}</p>
    </td>
  </tr>
</table>";
        }

        public static string Separador()
        {
            return @"<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin:4px 0 18px;"">
  <tr><td style=""border-top:1px solid #f1f5f9;""></td></tr>
</table>";
        }

        public static string Parrafo(string texto)
        {
            return $@"<p style=""margin:0 0 14px;font-family:Helvetica,Arial,sans-serif;font-size:14px;line-height:1.6;color:#475569;"">{texto}</p>";
        }

        public static string TextoEstado(string mensaje, TipoNotificacion tipo)
        {
            var (bg, left, color) = tipo switch
            {
                TipoNotificacion.Aprobacion => ("#f0fdf4", "#16a34a", "#15803d"),
                TipoNotificacion.Recepcion => ("#eff6ff", "#0284c7", "#1d4ed8"),
                TipoNotificacion.Rechazo => ("#fef2f2", "#dc2626", "#b91c1c"),
                TipoNotificacion.Actualizacion => ("#fff7ed", "#ea580c", "#c2410c"),
                TipoNotificacion.Eliminado => ("#fef2f2", "#dc2626", "#b91c1c"),
                TipoNotificacion.PreAutorizacion => ("#fff7ed", "#ea580c", "#c2410c"),
                _ => ("#eff6ff", "#2563eb", "#1d4ed8")
            };

            return $@"<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-top:20px;"">
  <tr>
    <td style=""background:{bg};border-left:4px solid {left};padding:12px 16px;border-radius:0 6px 6px 0;"">
      <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:13px;font-weight:600;color:{color};"">{mensaje}</p>
    </td>
  </tr>
</table>";
        }

        public static string BloqueObservacion(string observacion, TipoNotificacion tipo)
        {
            var (bg, border, labelColor, textColor) = tipo switch
            {
                TipoNotificacion.Rechazo => ("#fff5f5", "#dc2626", "#dc2626", "#7f1d1d"),
                TipoNotificacion.Aprobacion => ("#f0fdf4", "#16a34a", "#16a34a", "#14532d"),
                TipoNotificacion.Recepcion => ("#eff6ff", "#0284c7", "#0284c7", "#1e3a5f"),
                _ => ("#f8fafc", "#2563eb", "#2563eb", "#1e3a6e")
            };

            var etiqueta = tipo switch
            {
                TipoNotificacion.Rechazo => "MOTIVO DE RECHAZO",
                TipoNotificacion.Aprobacion => "OBSERVACIÓN",
                TipoNotificacion.Recepcion => "NOTA DE RECEPCIÓN",
                _ => "OBSERVACIÓN"
            };

            return $@"<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-top:20px;"">
  <tr>
    <td style=""background:{bg};border-left:4px solid {border};padding:14px 16px;border-radius:0 6px 6px 0;"">
      <p style=""margin:0 0 5px;font-family:Helvetica,Arial,sans-serif;font-size:9px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;color:{labelColor};"">{etiqueta}</p>
      <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:13px;line-height:1.6;color:{textColor};"">{observacion}</p>
    </td>
  </tr>
</table>";
        }

        public static string TablaProductosSolicitud(
            IEnumerable<(string Descripcion, string Proveedor, decimal Cantidad, string Unidad)> productos)
        {
            var sb = new StringBuilder();
            sb.Append(@"<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-top:16px;border-collapse:collapse;"">
  <thead>
    <tr style=""background:#1e293b;"">
      <th style=""padding:9px 12px;text-align:left;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">Descripción</th>
      <th style=""padding:9px 12px;text-align:left;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">Proveedor</th>
      <th style=""padding:9px 12px;text-align:center;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">Cant.</th>
      <th style=""padding:9px 12px;text-align:center;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">Unidad</th>
    </tr>
  </thead>
  <tbody>");

            var i = 0;
            foreach (var p in productos)
            {
                var bg = i % 2 == 0 ? "#ffffff" : "#f8fafc";
                sb.Append($@"
    <tr style=""background:{bg};"">
      <td style=""padding:10px 12px;font-family:Helvetica,Arial,sans-serif;font-size:13px;color:#1e293b;border-bottom:1px solid #f1f5f9;"">{p.Descripcion}</td>
      <td style=""padding:10px 12px;font-family:Helvetica,Arial,sans-serif;font-size:13px;color:#64748b;border-bottom:1px solid #f1f5f9;"">{p.Proveedor}</td>
      <td style=""padding:10px 12px;font-family:Helvetica,Arial,sans-serif;font-size:13px;color:#475569;border-bottom:1px solid #f1f5f9;text-align:center;"">{p.Cantidad:F2}</td>
      <td style=""padding:10px 12px;font-family:Helvetica,Arial,sans-serif;font-size:13px;color:#475569;border-bottom:1px solid #f1f5f9;text-align:center;"">{p.Unidad}</td>
    </tr>");
                i++;
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        // ── Plantillas predefinidas ──────────────────────────────────────────────

        public static string NotificacionCreada(
        string numeroOrden,
        string proveedor,
        string fecha,
        IEnumerable<(string NumeroSolicitud, string Solicitante)>? solicitudes = null)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de orden", numeroOrden));
            sb.Append(FilaDato("Fecha", fecha));
            sb.Append(FilaDato("Proveedor", proveedor));

            if (solicitudes != null && solicitudes.Any())
            {
                sb.Append(Separador());
                sb.Append(@"<p style=""margin:0 0 4px;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:600;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">Solicitudes vinculadas</p>");
                sb.Append(TablaSolicitudesVinculadas(solicitudes));
            }

            sb.Append(TextoEstado("Su solicitud ha sido convertida en una orden de compra.", TipoNotificacion.Normal));
            return sb.ToString();
        }

        public static string NotificacionConProductos(
            string numeroSolicitud,
            string fecha,
            string referencia,
            string solicitante,
            IEnumerable<(string Descripcion, string Proveedor, decimal Cantidad, string Unidad)> productos)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de solicitud", numeroSolicitud));
            sb.Append(FilaDato("Fecha", fecha));
            sb.Append(FilaDato("Referencia", referencia));
            sb.Append(FilaDato("Solicitante", solicitante));
            sb.Append(Separador());
            sb.Append(TablaProductosSolicitud(productos));
            sb.Append(TextoEstado("Nueva solicitud de compra registrada en el sistema.", TipoNotificacion.Normal));
            return sb.ToString();
        }

        public static string NotificacionAprobacion(string numeroOrden, string proveedor, string fecha)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de orden", numeroOrden));
            sb.Append(FilaDato("Fecha", fecha));
            sb.Append(FilaDato("Proveedor", proveedor));
            sb.Append(TextoEstado("La orden ha sido aprobada.", TipoNotificacion.Aprobacion));
            return sb.ToString();
        }
        public static string NotificacionPreAutorizacion(string numeroOrden, string proveedor, string fecha, string solicitantes)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de orden", numeroOrden));
            sb.Append(FilaDato("Fecha", fecha));
            sb.Append(FilaDato("Proveedor", proveedor));
            sb.Append(FilaDato("Solicitante(s)", solicitantes));
            sb.Append(TextoEstado("La orden está pendiente de su aprobación.", TipoNotificacion.PreAutorizacion));
            return sb.ToString();
        }
        public static string NotificacionRecepcion(string numeroOrden, string proveedor, string fechaRecepcion)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de orden", numeroOrden));
            sb.Append(FilaDato("Proveedor", proveedor));
            sb.Append(FilaDato("Fecha de recepción", fechaRecepcion));
            sb.Append(BloqueObservacion("Los productos han sido recibidos y confirmados en almacén.", TipoNotificacion.Recepcion));
            sb.Append(TextoEstado("Productos recibidos en almacén correctamente.", TipoNotificacion.Recepcion));
            return sb.ToString();
        }

        public static string NotificacionEliminada(string numeroSolicitud, string fecha, string referencia, string solicitante)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de solicitud", numeroSolicitud));
            sb.Append(FilaDato("Fecha", fecha));
            sb.Append(FilaDato("Referencia", referencia));
            sb.Append(FilaDato("Solicitante", solicitante));
            sb.Append(TextoEstado("La solicitud ha sido eliminada del sistema.", TipoNotificacion.Rechazo));
            return sb.ToString();
        }
        public static string NotificacionActualizada(
        string numeroSolicitud, string fecha, string referencia, string solicitante,
        IEnumerable<(string Descripcion, string Proveedor, decimal Cantidad, string Unidad)> productos)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de solicitud", numeroSolicitud));
            sb.Append(FilaDato("Fecha", fecha));
            sb.Append(FilaDato("Referencia", referencia));
            sb.Append(FilaDato("Solicitante", solicitante));
            sb.Append(Separador());
            sb.Append(TablaProductosSolicitud(productos));
            sb.Append(TextoEstado("La solicitud de compra fue actualizada.", TipoNotificacion.Actualizacion));
            return sb.ToString();
        }

        public static string NotificacionRechazo(string numeroOrden, string proveedor, string motivo)
        {
            var sb = new StringBuilder();
            sb.Append(FilaDato("Número de orden", numeroOrden));
            sb.Append(FilaDato("Proveedor", proveedor));
            sb.Append(BloqueObservacion(motivo, TipoNotificacion.Rechazo));
            sb.Append(TextoEstado("La orden ha sido rechazada. Revise el motivo indicado.", TipoNotificacion.Rechazo));
            return sb.ToString();
        }
        public static string TablaSolicitudesVinculadas(
    IEnumerable<(string NumeroSolicitud, string Solicitante)> solicitudes)
        {
            var sb = new StringBuilder();
            sb.Append(@"<table cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-top:16px;border-collapse:collapse;"">
              <thead>
                <tr style=""background:#1e293b;"">
                  <th style=""padding:9px 12px;text-align:left;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">N° Solicitud</th>
                  <th style=""padding:9px 12px;text-align:left;font-family:Helvetica,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#94a3b8;"">Solicitante</th>
                </tr>
              </thead>
              <tbody>");

            var i = 0;
            foreach (var s in solicitudes)
            {
                var bg = i % 2 == 0 ? "#ffffff" : "#f8fafc";
                sb.Append($@"
                <tr style=""background:{bg};"">
                  <td style=""padding:10px 12px;font-family:Helvetica,Arial,sans-serif;font-size:13px;color:#1e293b;border-bottom:1px solid #f1f5f9;"">{s.NumeroSolicitud}</td>
                  <td style=""padding:10px 12px;font-family:Helvetica,Arial,sans-serif;font-size:13px;color:#64748b;border-bottom:1px solid #f1f5f9;"">{s.Solicitante}</td>
                </tr>");
                i++;
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }
}