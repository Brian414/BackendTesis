using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBackend.DataBase;
using MyBackend.Model;

namespace MyBackend.Controller;
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase{
    private readonly DBContext context;
    public UserController(DBContext dBContext){
        context = dBContext;
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
            Password = userModel.Password,
        };
        await context.Users.AddAsync(newUser);
        await context.SaveChangesAsync();
        return Ok(new {message ="Usuario creado exitosamente "});
    }
}