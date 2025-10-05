using BusinessObjects.Models;
using DataAccess.DataAccessLayer;
using Microsoft.EntityFrameworkCore;


namespace Repositories.Repositories
{
 // NewsArticleRepository.cs
	public class NewsArticleRepository : INewsArticleRepository
	{
		private readonly NewsArticleDAO _dao;

        public NewsArticleRepository(NewsArticleDAO dao)
        { 
            _dao = dao; 
        }

        public async Task<List<NewsArticle>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<NewsArticle?> GetByIdAsync(string id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task AddAsync(NewsArticle newsArticle)
        {
            await _dao.AddAsync(newsArticle);
        }

        public async Task UpdateAsync(NewsArticle newsArticle)
        {
            await _dao.UpdateAsync(newsArticle);
        }

        public async Task DeleteAsync(string id)
        {
            await _dao.DeleteAsync(id);
        }
	}
}
