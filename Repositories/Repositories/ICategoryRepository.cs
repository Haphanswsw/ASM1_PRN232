using BusinessObjects.Models;


namespace Repositories.Repositories
{
 // ICategoryRepository.cs
	public interface ICategoryRepository
	{

        Task<List<Category>> GetAllAsync();

        Task<Category?> GetByIdAsync(short id);

        Task AddAsync(Category category);

        Task UpdateAsync(Category category);

        Task DeleteAsync(short id);
	}
}
