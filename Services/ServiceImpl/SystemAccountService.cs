using BusinessObjects.Models;
using Repositories;
using Repositories.RepositoryImpl;

namespace Services.ServiceImpl;

public class SystemAccountService : ISystemAccountService
{
    private readonly ISystemAccountRepository _systemAccountRepository;

    public SystemAccountService(ISystemAccountRepository systemAccountRepository)
    {
        _systemAccountRepository =
            systemAccountRepository
            ?? throw new ArgumentNullException(nameof(systemAccountRepository));
    }

    public Task<IEnumerable<SystemAccount>> GetAllAsync()
    {
        return _systemAccountRepository.GetAllAsync();
    }

    public Task<SystemAccount?> GetByIdAsync(short id)
    {
        return _systemAccountRepository.GetByIdAsync(id);
    }

    public Task<SystemAccount> AddAsync(SystemAccount entity)
    {
        return _systemAccountRepository.AddAsync(entity);
    }

    public Task<SystemAccount> UpdateAsync(SystemAccount entity)
    {
        return _systemAccountRepository.UpdateAsync(entity);
    }

    public Task DeleteAsync(short id)
    {
        return _systemAccountRepository.DeleteAsync(id);
    }

    public SystemAccount? Login(string accountEmail, string accountPassword)
    {
        return _systemAccountRepository
            .GetQueryable()
            .FirstOrDefault(e =>
                e.AccountEmail == accountEmail && e.AccountPassword == accountPassword
            );
    }

    public SystemAccount? FindByEmail(string email)
    {
        return _systemAccountRepository
            .GetQueryable()
            .FirstOrDefault(e => e!.AccountEmail == email);
    }
}
