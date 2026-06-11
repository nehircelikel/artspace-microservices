namespace artspace.API.DTOs;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Artist" veya "Visitor"
    public string? Bio { get; set; }
    public string? ContactEmail { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ContactEmail { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserResponseDto User { get; set; } = null!;
}

// Public-safe profile projection used by the by-username lookup. Deliberately
// omits the login Email (PII) — ContactEmail is the artist's public contact.
public class PublicProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ContactEmail { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Self-service profile edit; all fields optional for partial update.
public class UpdateProfileDto
{
    public string? Bio { get; set; }
    public string? ContactEmail { get; set; }
    public string? ProfilePictureUrl { get; set; }
}