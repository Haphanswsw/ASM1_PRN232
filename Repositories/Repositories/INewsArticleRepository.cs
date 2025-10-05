using BusinessObjects.Models;


namespace Repositories.Repositories
{
 // INewsArticleRepository.cs
	public interface INewsArticleRepository
	{

        Task<List<NewsArticle>> GetAllAsync();

        Task<NewsArticle?> GetByIdAsync(string id);

        Task AddAsync(NewsArticle newsArticle);

        Task UpdateAsync(NewsArticle newsArticle);

        Task DeleteAsync(string id);
	}
}
