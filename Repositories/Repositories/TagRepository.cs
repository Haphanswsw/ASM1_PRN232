using BusinessObjects.Models;
using DataAccess.DataAccessLayer;
using Microsoft.EntityFrameworkCore;


namespace Repositories.Repositories
{
 // TagRepository.cs
	public class TagRepository : ITagRepository
	{
		private readonly TagDAO _dao;

        public TagRepository(TagDAO dao)
        { 
            _dao = dao; 
        }

        public async Task<List<Tag>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task AddAsync(Tag tag)
        {
            await _dao.AddAsync(tag);
        }

        public async Task UpdateAsync(Tag tag)
        {
            await _dao.UpdateAsync(tag);
        }

        public async Task DeleteAsync(int id)
        {
            await _dao.DeleteAsync(id);
        }
	}
}
