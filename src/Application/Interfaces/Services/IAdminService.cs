namespace Infrastructure.Interfaces.Services;

public interface IAdminService
{
    Task Seed(CancellationToken cancellationToken = default);

    Task Reset(CancellationToken cancellationToken = default);
}