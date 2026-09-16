using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SistemaFacturacion.API.Services
{
    public class CorreoService
    {
        private readonly IConfiguration _config;

        public CorreoService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendInvoiceEmailAsync(string destinatario, string numeroFactura, byte[] pdfBytes)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Facturación Electrónica", _config["Smtp:User"] ?? "facturacion@sistemafactura.com"));
                message.To.Add(new MailboxAddress("", destinatario));
                message.Subject = $"Factura Electrónica Emitida #{numeroFactura}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #1E293B;'>
                        <h2 style='color: #4F46E5;'>Comprobante Fiscal Digital (CFDI)</h2>
                        <p>Estimado(a) Cliente,</p>
                        <p>Adjunto a este correo encontrará su factura electrónica correspondiente al folio <strong>#{numeroFactura}</strong> en formato PDF.</p>
                        <br/>
                        <p>Agradecemos su preferencia.</p>
                        <hr/>
                        <small style='color: #64748B;'>Este es un mensaje automático generado por el Sistema de Facturación Electrónica.</small>
                    </div>"
                };

                bodyBuilder.Attachments.Add($"Factura_{numeroFactura}.pdf", pdfBytes, ContentType.Parse("application/pdf"));
                message.Body = bodyBuilder.ToMessageBody();

                // Intentar enviar mediante SMTP configurado, o simular con log exitoso si no hay servidor real configurado
                var host = _config["Smtp:Host"];
                if (!string.IsNullOrEmpty(host))
                {
                    using var client = new SmtpClient();
                    var port = int.Parse(_config["Smtp:Port"] ?? "587");
                    await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                    var user = _config["Smtp:User"] ?? string.Empty;
                    var pass = _config["Smtp:Password"] ?? string.Empty;
                    await client.AuthenticateAsync(user, pass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                else
                {
                    // Simulación de envío exitoso para desarrollo
                    Console.WriteLine($"[EMAIL SIMULADO] Factura #{numeroFactura} enviada a {destinatario} ({pdfBytes.Length} bytes PDF)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] Error enviando correo: {ex.Message}");
            }
        }
    }
}
