using BusinessObjects.Models;
using DataAccess.DataAccessLayer;
using Microsoft.EntityFrameworkCore;


namespace Repositories.Repositories
{
 // CategoryRepository.cs
	public class CategoryRepository : ICategoryRepository
	{
		private readonly CategoryDAO _dao;

        public CategoryRepository(CategoryDAO dao)
        { 
            _dao = dao; 
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(short id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task AddAsync(Category category)
        {
            await _dao.AddAsync(category);
        }

        public async Task UpdateAsync(Category category)
        {
            await _dao.UpdateAsync(category);
        }

        public async Task DeleteAsync(short id)
        {
            await _dao.DeleteAsync(id);
        }
	}
}
