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
    //para crear un nuevo usuario y que este sea consultor hay que registrarlo directamente desde el backend i poner la variable esConsultor en true
    public async Task<IActionResult> CreateUser(UserModel userModel){
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(userModel.Email));
        if(user is not null){
            return BadRequest(new {message = "El usuario ya esta registrado"});
        }
        
        // Generar código de verificación y enviar correo de verificación
        string verificationCode = _emailService.SendVerificationEmail(
            userModel.Email
        );
            
        var newUser = new User{
            Email = userModel.Email,
            Name = userModel.Name,
            Password = _passwordService.HashPassword(userModel.Password),
            Code = verificationCode,
            EsConsultor = userModel.EsConsultor ?? false, // Si es null, será false
        };
        await context.Users.AddAsync(newUser);
        await context.SaveChangesAsync();
        return Ok(new {message ="Usuario creado exitosamente."});
    }
    
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(loginModel.Email));
        
        if (user is null || !_passwordService.VerifyPassword(loginModel.Password, user.Password))
        {
            return BadRequest(new { message = "Email o contraseña incorrectos" });
        }
        
        var token = _tokenService.GenerateToken(user.UserId, user.EsConsultor);
        
        // Agregar el token al header de la respuesta
        Response.Headers.Add("Authorization", $"Bearer {token}");
        
        return Ok(new { 
            message = "Login exitoso. Para usar en Swagger: Copia el token y añádelo en el candado con el formato 'Bearer <token>'",
            token = token,
            tokenType = "Bearer",
            user = new {
                userId = user.UserId,
                name = user.Name,
                email = user.Email,
                esConsultor = user.EsConsultor
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
        
        // Generar código de verificación y enviar correo de restablecimiento
        string resetCode = _emailService.SendPasswordResetEmail(
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

    [HttpDelete("ClearAllUsers")]
    public async Task<IActionResult> ClearAllUsers([FromQuery] string adminKey)
    {
        // Aquí deberías definir una clave de administrador segura en tu configuración
        string expectedAdminKey = "12345678"; 
        
        if (adminKey != expectedAdminKey)
        {
            return Unauthorized(new { message = "No autorizado para realizar esta acción" });
        }

        await context.Users.ExecuteDeleteAsync();
        await context.SaveChangesAsync();
        
        return Ok(new { message = "Tabla de usuarios vaciada correctamente" });
    }

    [HttpGet("GetConsultores")]
    //este metodo se usa para obtener la lista de los consultores tomar el id y ponerlo manualmente en el fronted 
    public async Task<IActionResult> GetConsultores()
    {
        var consultores = await context.Users
            .Where(u => u.EsConsultor)
            .Select(u => new {
                Id = u.UserId,
                Nombre = u.Name
            })
            .ToListAsync();

        if (!consultores.Any())
        {
            return NotFound(new { message = "No se encontraron consultores" });
        }

        return Ok(consultores);
    }
}