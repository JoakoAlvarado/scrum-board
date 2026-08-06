using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ScrumBoard.Api.Hubs;

/// <summary>
/// Canal de tiempo real del tablero (requisito 6.7). Autenticado con el mismo JWT de
/// sesión (ver configuración de JwtBearerEvents.OnMessageReceived en Program.cs, que
/// acepta el token por query string para el handshake de WebSocket).
///
/// Un grupo de SignalR por proyecto: una sesión solo recibe eventos de los tableros a
/// los que se suscribió explícitamente, nunca de todos los proyectos.
/// </summary>
[Authorize]
public class TableroHub : Hub
{
    public static string NombreGrupo(Guid proyectoId) => $"proyecto-{proyectoId}";

    public async Task SuscribirseAProyecto(Guid proyectoId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, NombreGrupo(proyectoId));
    }

    public async Task DesuscribirseDeProyecto(Guid proyectoId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NombreGrupo(proyectoId));
    }

    // No hace falta limpiar grupos en OnDisconnectedAsync: SignalR remueve la conexión
    // de todos sus grupos automáticamente al desconectarse (evita conexiones huérfanas).
}
