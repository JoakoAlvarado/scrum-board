using ScrumBoard.Application.Ports;
using ScrumBoard.Domain.Entities;

namespace ScrumBoard.Infrastructure.Persistence.Seed;

/// <summary>
/// Migración semilla: crea al menos los dos usuarios precargados que exige el
/// enunciado (sección 6.2), con password hasheada (BCrypt + pepper).
/// Se ejecuta una sola vez al levantar la Api (idempotente).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ScrumBoardDbContext context, IPasswordHasher passwordHasher)
    {
        if (context.Usuarios.Any())
            return;

        var admin = new Usuario("Admin Demo", "admin@scrumboard.local", passwordHasher.Hash("Admin123!"));
        var miembro = new Usuario("Usuario Demo", "usuario@scrumboard.local", passwordHasher.Hash("Usuario123!"));

        context.Usuarios.AddRange(admin, miembro);
        await context.SaveChangesAsync();
    }
}
