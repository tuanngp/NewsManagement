using BusinessObjects.Models;

namespace Services;

public interface ISystemAccountService : IService<SystemAccount, short>
{
    SystemAccount? FindByEmail(string email);
    SystemAccount? Login(string accountEmail, string accountPassword);
}
