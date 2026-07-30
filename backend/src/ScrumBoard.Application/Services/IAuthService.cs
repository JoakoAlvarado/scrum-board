using ScrumBoard.Application.Dtos;

namespace ScrumBoard.Application.Services;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(string email, string password, CancellationToken ct = default);
}
