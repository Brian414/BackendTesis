using System;
using System.Net;
using System.Net.Mail;
using MyBackend.Interface;

namespace MyBackend.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _password;
    private readonly string _baseUrl;

    public EmailService(IConfiguration configuration)
    {
        _smtpServer = configuration["Email:SmtpServer"];
        _smtpPort = int.Parse(configuration["Email:Port"]);
        _fromEmail = configuration["Email:Username"];
        _password = configuration["Email:Password"];
        _baseUrl = configuration["BaseUrl"];
    }

    public string SendVerificationEmail(string email)
{
    try
    {
        string verificationCode = GenerateVerificationCode();
        // Codificar el email para la URL
        string encodedEmail = Uri.EscapeDataString(email);
        string verificationLink = $"{_baseUrl}/auth/verify-email?email={encodedEmail}&code={verificationCode}";
        
        string subject = "Verifica tu cuenta";
        string body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #333; margin-bottom: 20px;'>Bienvenido a nuestra web de Servicios de Consultoria de ETECSA</h2>
                    <p>Para completar tu registro, por favor verifica tu cuenta:</p>
                    <div style='margin: 25px 0;'>
                        <a href='{verificationLink}' 
                           style='display: inline-block; padding: 12px 24px; 
                                  background-color: #4CAF50; color: white; 
                                  text-decoration: none; border-radius: 5px;
                                  font-weight: bold;'>
                            Verificar mi cuenta
                        </a>
                    </div>
                    <p> Una vez verifiques tu cuenta, inicia sesión y accede a todos nuestros servicios.</p

                    <p>Si no solicitaste este registro, ignora este correo.</p>
                
                </div>
            </body>
            </html>";

        SendEmailMessage(email, subject, body);
        Console.WriteLine($"Correo de verificación enviado exitosamente a: {email}");
        return verificationCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al enviar correo de verificación: {ex.Message}");
        throw new Exception($"Error al enviar correo de verificación: {ex.Message}", ex);
    }
}
    public string SendPasswordResetEmail(string email)
    {
        string resetCode = GenerateVerificationCode();
        
        string subject = "Restablecimiento de contraseña";
        string body = $@"
            <html>
            <body>
                <h2>Restablecimiento de contraseña</h2>
                <p>Has solicitado restablecer tu contraseña.</p>
                <p>Tu código de verificación es: <strong>{resetCode}</strong></p>
                <p>Este código expirará en 1 hora.</p>
                <p>Si no solicitaste este cambio, ignora este correo.</p>
            </body>
            </html>";

        SendEmailMessage(email, subject, body);
        return resetCode;
    }

    private void SendEmailMessage(string to, string subject, string body)
    {
        var smtpClient = new SmtpClient
        {
            Host = _smtpServer,
            Port = _smtpPort,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_fromEmail, _password)
        };

        using (var mailMessage = new MailMessage())
        {
            mailMessage.From = new MailAddress(_fromEmail);
            mailMessage.To.Add(to);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al enviar el correo electrónico: {ex.Message}", ex);
            }
        }
    }

    private string GenerateVerificationCode()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}