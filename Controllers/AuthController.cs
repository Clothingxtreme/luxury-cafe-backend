using Microsoft.AspNetCore.Mvc;
using MaisonGlace.API.Models;
using MaisonGlace.API.Services;

namespace MaisonGlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        string? token;
        try
        {
            token = await _auth.LoginAsync(request.Email, request.Password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth login failed due to backend dependency. Email: {Email}", request.Email);
            return StatusCode(503, new { message = "Auth service unavailable" });
        }

        if (token is null)
        {
            _logger.LogWarning("Auth login denied. Email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        _logger.LogInformation("Auth login success. Email: {Email}", request.Email);

        return Ok(new LoginResponse
        {
            Token = token,
            Email = request.Email,
            Message = "Login successful",
        });
    }
}
