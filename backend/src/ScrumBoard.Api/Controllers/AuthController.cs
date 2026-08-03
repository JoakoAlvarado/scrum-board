using Microsoft.AspNetCore.Mvc;
using ScrumBoard.Application.Services;

namespace ScrumBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        // CredencialesInvalidasException se traduce a 401 en ExceptionHandlingMiddleware.
        var resultado = await _authService.LoginAsync(request.Email, request.Password, ct);
        return Ok(resultado);
    }
}
