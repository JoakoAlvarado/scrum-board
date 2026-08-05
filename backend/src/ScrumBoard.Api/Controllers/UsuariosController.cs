using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrumBoard.Application.Dtos;
using ScrumBoard.Application.Services;

namespace ScrumBoard.Api.Controllers;

/// <summary>
/// Listado de usuarios, exclusivamente para poblar el selector de "responsable" al
/// crear/editar una tarea. No forma parte del modelo de dominio mínimo del enunciado;
/// es un endpoint de apoyo mínimo, sin alta/edición/baja (fuera de alcance del challenge).
/// </summary>
[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService) => _usuarioService = usuarioService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken ct)
    {
        return Ok(await _usuarioService.ListarAsync(ct));
    }
}
