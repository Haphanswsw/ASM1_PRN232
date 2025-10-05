using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;


namespace DataAccess.DataAccessLayer
{
 // TagDAO.cs
	public class TagDAO
	{
		private readonly FunewsManagementContext _context;

        public TagDAO(FunewsManagementContext context)
        { 
            _context = context; 
        }

        public async Task<List<Tag>> GetAllAsync()
        {
            return await _context.Tags
                .ToListAsync();
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(c => c.TagId == id);
        }

        public async Task AddAsync(Tag tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tag tag)
        {
            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            }
        }
	}
}
