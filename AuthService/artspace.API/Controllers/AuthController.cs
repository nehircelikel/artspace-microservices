using artspace.API.DTOs;
using artspace.Core.Interfaces;
using artspace.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

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
                dto.Role, dto.Bio, dto.ContactEmail
            );

            var token = await _authService.LoginAsync(dto.Email, dto.Password);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role,
                    Bio = user.Bio,
                    ContactEmail = user.ContactEmail,
                    CreatedAt = user.CreatedAt
                }
            });
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

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = new UserResponseDto
                {
                    Id = user!.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role,
                    Bio = user.Bio,
                    ContactEmail = user.ContactEmail,
                    CreatedAt = user.CreatedAt
                }
            });
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

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role,
            Bio = user.Bio,
            ContactEmail = user.ContactEmail,
            CreatedAt = user.CreatedAt
        });
    }
}