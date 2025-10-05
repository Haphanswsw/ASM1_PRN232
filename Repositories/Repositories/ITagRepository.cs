using BusinessObjects.Models;


namespace Repositories.Repositories
{
 // ITagRepository.cs
	public interface ITagRepository
	{

        Task<List<Tag>> GetAllAsync();

        Task<Tag?> GetByIdAsync(int id);

        Task AddAsync(Tag tag);

        Task UpdateAsync(Tag tag);

        Task DeleteAsync(int id);
	}
}
