using System.ComponentModel.DataAnnotations;

namespace MyBackend.Model;
public class User{
    [Key]
    public Guid UserId { get ; set ;}
    public required string Name { get ; set ;}
    public required string Password { get ; set ;}
    public required string Email { get ; set ; }
    public string? Code { get; set; }
    public DateTime? EmailVerificado { get; set; }
    public bool EsConsultor { get; set; } = false; // Valor por defecto false
}
