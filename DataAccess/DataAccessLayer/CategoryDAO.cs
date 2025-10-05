using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;


namespace DataAccess.DataAccessLayer
{
 // CategoryDAO.cs
	public class CategoryDAO
	{
		private readonly FunewsManagementContext _context;

        public CategoryDAO(FunewsManagementContext context)
        { 
            _context = context; 
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(short id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(short id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
	}
}
