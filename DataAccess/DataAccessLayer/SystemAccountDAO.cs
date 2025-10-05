using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;


namespace DataAccess.DataAccessLayer
{
 // SystemAccountDAO.cs
	public class SystemAccountDAO
	{
		private readonly FunewsManagementContext _context;

        public SystemAccountDAO(FunewsManagementContext context)
        { 
            _context = context; 
        }

        public async Task<List<SystemAccount>> GetAllAsync()
        {
            return await _context.SystemAccounts
                .ToListAsync();
        }

        public async Task<SystemAccount?> GetByIdAsync(short id)
        {
            return await _context.SystemAccounts
                .FirstOrDefaultAsync(c => c.AccountId == id);
        }

        public async Task AddAsync(SystemAccount systemAccount)
        {
            _context.SystemAccounts.Add(systemAccount);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SystemAccount systemAccount)
        {
            _context.SystemAccounts.Update(systemAccount);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(short id)
        {
            var systemAccount = await _context.SystemAccounts.FindAsync(id);
            if (systemAccount != null)
            {
                _context.SystemAccounts.Remove(systemAccount);
                await _context.SaveChangesAsync();
            }
        }
	}
}
