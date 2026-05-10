using Microsoft.AspNetCore.Mvc;
using MaisonGlace.API.Models;
using MaisonGlace.API.Services;

namespace MaisonGlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var token = await _auth.LoginAsync(request.Email, request.Password);

        if (token is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(new LoginResponse
        {
            Token = token,
            Email = request.Email,
            Message = "Login successful",
        });
    }
}
