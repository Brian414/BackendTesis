namespace MyBackend.Class;

public class UserModel
{
    public required string Name { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public bool? EsConsultor { get; set; }  // Ahora es bool?
}