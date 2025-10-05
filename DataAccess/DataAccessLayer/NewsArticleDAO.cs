using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;


namespace DataAccess.DataAccessLayer
{
 // NewsArticleDAO.cs
	public class NewsArticleDAO
	{
		private readonly FunewsManagementContext _context;

        public NewsArticleDAO(FunewsManagementContext context)
        { 
            _context = context; 
        }

        public async Task<List<NewsArticle>> GetAllAsync()
        {
            return await _context.NewsArticles
                .ToListAsync();
        }

        public async Task<NewsArticle?> GetByIdAsync(string id)
        {
            return await _context.NewsArticles
                .FirstOrDefaultAsync(c => c.NewsArticleId == id);
        }

        public async Task AddAsync(NewsArticle newsArticle)
        {
            _context.NewsArticles.Add(newsArticle);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NewsArticle newsArticle)
        {
            _context.NewsArticles.Update(newsArticle);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var newsArticle = await _context.NewsArticles.FindAsync(id);
            if (newsArticle != null)
            {
                _context.NewsArticles.Remove(newsArticle);
                await _context.SaveChangesAsync();
            }
        }
	}
}
