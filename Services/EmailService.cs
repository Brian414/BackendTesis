using System;
using System.Net;
using System.Net.Mail;
using MyBackend.Interface;

namespace MyBackend.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string SendEmail(string to ){
        string token = GenerateVerificationCode();
        MailMessage mailMessage = new MailMessage("yaselbarrioscarrillo@gmail.com" , to , "Yasel" ,"http://localhost:5194/api/User/VerifyEmail?email="+to+"&code="+token);
        mailMessage.IsBodyHtml = true;
        System.Net.Mail.SmtpClient smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com");
        smtpClient.EnableSsl = true;
        smtpClient.UseDefaultCredentials = false;
        smtpClient.Port = Convert.ToInt32(_configuration.GetSection("Email:Port").Value);
        smtpClient.Credentials = new System.Net.NetworkCredential(_configuration.GetSection("Email:Username").Value , _configuration.GetSection("Email:Password").Value);
        smtpClient.Send(mailMessage);
        return token;
    }
    
    
    private string GenerateVerificationCode()
    {
        // Generar un código aleatorio de 6 dígitos
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}