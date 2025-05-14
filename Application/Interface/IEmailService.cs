namespace MyBackend.Interface;

public interface IEmailService
{
    //cambios 
     string SendVerificationEmail(string email);
    string SendPasswordResetEmail(string email);
}