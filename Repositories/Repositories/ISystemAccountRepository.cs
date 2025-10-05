using BusinessObjects.Models;


namespace Repositories.Repositories
{
 // ISystemAccountRepository.cs
	public interface ISystemAccountRepository
	{

        Task<List<SystemAccount>> GetAllAsync();

        Task<SystemAccount?> GetByIdAsync(short id);

        Task AddAsync(SystemAccount systemAccount);

        Task UpdateAsync(SystemAccount systemAccount);

        Task DeleteAsync(short id);
	}
}
