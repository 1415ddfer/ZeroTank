namespace TestTank.Business.Login;

public interface IAccountRepository
{
    Task<UserAccount?> GetAccountAsync(string username);
    Task SaveUserAccountAsync(UserAccount userAccount);
    Task InitializeIndexesAsync();
}