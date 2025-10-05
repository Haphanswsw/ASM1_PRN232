using BusinessObjects.Models;
using DataAccess.DataAccessLayer;
using Microsoft.EntityFrameworkCore;


namespace Repositories.Repositories
{
 // SystemAccountRepository.cs
	public class SystemAccountRepository : ISystemAccountRepository
	{
		private readonly SystemAccountDAO _dao;

        public SystemAccountRepository(SystemAccountDAO dao)
        { 
            _dao = dao; 
        }

        public async Task<List<SystemAccount>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<SystemAccount?> GetByIdAsync(short id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task AddAsync(SystemAccount systemAccount)
        {
            await _dao.AddAsync(systemAccount);
        }

        public async Task UpdateAsync(SystemAccount systemAccount)
        {
            await _dao.UpdateAsync(systemAccount);
        }

        public async Task DeleteAsync(short id)
        {
            await _dao.DeleteAsync(id);
        }
	}
}
