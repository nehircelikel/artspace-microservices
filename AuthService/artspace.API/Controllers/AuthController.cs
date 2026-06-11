using artspace.API.DTOs;
using artspace.Core.Entities;
using artspace.Core.Interfaces;
using artspace.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace artspace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IUserRepository _userRepository;

    public AuthController(AuthService authService, IUserRepository userRepository)
    {
        _authService = authService;
        _userRepository = userRepository;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        try
        {
            var user = await _authService.RegisterAsync(
                dto.Email, dto.Password, dto.Username,
                dto.Role, dto.Bio, dto.ContactEmail, dto.ProfilePictureUrl
            );

            var token = await _authService.LoginAsync(dto.Email, dto.Password);

            return Ok(new AuthResponseDto { Token = token, User = MapToDto(user) });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        try
        {
            var token = await _authService.LoginAsync(dto.Email, dto.Password);
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            return Ok(new AuthResponseDto { Token = token, User = MapToDto(user!) });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("profile/{id}")]
    public async Task<ActionResult<UserResponseDto>> GetProfile(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(MapToDto(user));
    }

    // Public profile lookup used by the /users/{username} and /artists/{username}
    // pages. Returns a public-safe projection (no login email).
    [HttpGet("profile/by-username/{username}")]
    public async Task<ActionResult<PublicProfileDto>> GetProfileByUsername(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return NotFound();

        return Ok(new PublicProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            Bio = user.Bio,
            ContactEmail = user.ContactEmail,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt
        });
    }

    // Self-service edit of the logged-in user's own profile.
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        if (dto.Bio != null) user.Bio = dto.Bio;
        if (dto.ContactEmail != null) user.ContactEmail = dto.ContactEmail;
        if (dto.ProfilePictureUrl != null) user.ProfilePictureUrl = dto.ProfilePictureUrl;

        var updated = await _userRepository.UpdateAsync(user);
        return Ok(MapToDto(updated));
    }

    private static UserResponseDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Username = user.Username,
        Role = user.Role,
        Bio = user.Bio,
        ContactEmail = user.ContactEmail,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };
}
