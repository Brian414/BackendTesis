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
    
    public UserController(DBContext dBContext, IPasswordService passwordService, ITokenService tokenService){
        context = dBContext;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }
    [HttpPost("CreateUser")]
    public async Task<IActionResult> CreateUser(UserModel userModel){
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(userModel.Email));
        if(user is not null){
            return BadRequest(new {message = "El usuario ya esta registrado"});
        }
        var newUser = new User{
            Email = userModel.Email,
            Name = userModel.Name,
            Password = _passwordService.HashPassword(userModel.Password),
        };
        await context.Users.AddAsync(newUser);
        await context.SaveChangesAsync();
        return Ok(new {message ="Usuario creado exitosamente "});
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
}