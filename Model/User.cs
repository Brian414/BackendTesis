using System.ComponentModel.DataAnnotations;

namespace MyBackend.Model;
public class User{
    [Key]
    public Guid UserId { get ; set ;}
    public required string Name { get ; set ;}
    public required string Password { get ; set ;}
    public required string Email { get ; set ; }
}
