namespace ScrumBoard.Infrastructure.Security;

/// <summary>Se puebla desde variables de entorno (ver .env.example / appsettings) — nunca hardcodeada.</summary>
public class JwtOptions
{
    public const string SeccionConfig = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ScrumBoard.Api";
    public string Audience { get; set; } = "ScrumBoard.Client";
    public int ExpiracionMinutos { get; set; } = 60;
}
