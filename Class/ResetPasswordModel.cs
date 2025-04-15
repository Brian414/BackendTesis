namespace MyBackend.Class;

/// <summary>
/// Modelo para solicitar el restablecimiento de contraseña
/// </summary>
public class ResetPasswordRequestModel
{
    public required string Email { get; set; }
}


public class ResetPasswordVerifyModel
{
    public required string Email { get; set; }
    public required string Code { get; set; }
    public required string NewPassword { get; set; }
}

public class VerifyCode
{
    public required string Code { get; set; }

    public required string Email { get; set; }
}