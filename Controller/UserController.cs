using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBackend.DataBase;
using MyBackend.Interface;
using MyBackend.Model;
using MyBackend.Class;

namespace MyBackend.Controller;
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase{
    private readonly DBContext context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    
    public UserController(DBContext dBContext, IPasswordService passwordService, ITokenService tokenService, IEmailService emailService){
        context = dBContext;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailService = emailService;
    }
    
    [HttpPost("CreateUser")]
    public async Task<IActionResult> CreateUser(UserModel userModel){
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(userModel.Email));
        if(user is not null){
            return BadRequest(new {message = "El usuario ya esta registrado"});
        }
        
        // Generar código de verificación y enviar correo
        string verificationCode = _emailService.SendEmail(
            userModel.Email
        );
        
        var newUser = new User{
            Email = userModel.Email,
            Name = userModel.Name,
            Password = _passwordService.HashPassword(userModel.Password),
            Code = verificationCode
        };
        await context.Users.AddAsync(newUser);
        await context.SaveChangesAsync();
        return Ok(new {message ="Usuario creado exitosamente."});
    }
    
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(loginModel.Email));
        
        if (user is null)
        {
            return BadRequest(new { message = "Email o contraseña incorrectos" });
        }
        
        if (!_passwordService.VerifyPassword(loginModel.Password, user.Password))
        {
            return BadRequest(new { message = "Email o contraseña incorrectos" });
        }
        
        var token = _tokenService.GenerateToken(user.UserId);
        
        return Ok(new { 
            message = "Login exitoso",
            token = token,
            user = new {
                userId = user.UserId,
                name = user.Name,
                email = user.Email
            }
        });
    }
    
    [HttpGet("VerifyEmail")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string code)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(email));
        
        if (user is null)
        {
            return BadRequest(new { message = "Usuario no encontrado" });
        }
        
        if (user.Code != code)
        {
            return BadRequest(new { message = "Código de verificación incorrecto" });
        }
        
        if (user.EmailVerificado.HasValue)
        {
            return BadRequest(new { message = "El correo ya ha sido verificado" });
        }
        
        // Actualizar el campo EmailVerificado con la fecha actual
        user.EmailVerificado = DateTime.UtcNow;
        await context.SaveChangesAsync();
        
        return Ok(new { message = "Correo electrónico verificado correctamente" });
    }
    
    [HttpPost("RequestPasswordReset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] ResetPasswordRequestModel model)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(model.Email));
        
        if (user is null)
        {
            return BadRequest(new { message = "Usuario no encontrado" });
        }
        
        // Generar código de verificación y enviar correo
        string resetCode = _emailService.SendEmail(
            model.Email
        );
        
        // Guardar el código de verificación en el usuario
        user.Code = resetCode;
        await context.SaveChangesAsync();
        
        return Ok(new { message = "Se ha enviado un código de verificación a tu correo electrónico" });
    }
    
    [HttpPost("VerifyPasswordReset")]
    public async Task<IActionResult> VerifyPasswordReset([FromBody] ResetPasswordVerifyModel model)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(model.Email));
        
        if (user is null)
        {
            return BadRequest(new { message = "Usuario no encontrado" });
        }
        
        if (user.Code != model.Code)
        {
            return BadRequest(new { message = "Código de verificación incorrecto" });
        }
        
        // Actualizar la contraseña del usuario
        user.Password = _passwordService.HashPassword(model.NewPassword);
        user.Code = null; // Limpiar el código de verificación después de usarlo
        await context.SaveChangesAsync();
        
        return Ok(new { message = "Contraseña actualizada correctamente" });
    }

    [HttpPost("ValidateVerificationToken")]
    public async Task<IActionResult> ValidateVerificationToken([FromBody] VerifyCode model)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(model.Email));
        
        if (user.Code != model.Code)
        {
            return BadRequest(new { message = "Código de verificación incorrecto" });
        }
        
        return Ok(new { message = "Código de verificación válido", isValid = true });
    }
}