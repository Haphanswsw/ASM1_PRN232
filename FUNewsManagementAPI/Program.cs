using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FunewsManagementContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<DataAccess.DataAccessLayer.CategoryDAO>();
builder.Services.AddScoped<Repositories.Repositories.ICategoryRepository, Repositories.Repositories.CategoryRepository>();

builder.Services.AddScoped<DataAccess.DataAccessLayer.NewsArticleDAO>();
builder.Services.AddScoped<Repositories.Repositories.INewsArticleRepository, Repositories.Repositories.NewsArticleRepository>();

builder.Services.AddScoped<DataAccess.DataAccessLayer.SystemAccountDAO>();
builder.Services.AddScoped<Repositories.Repositories.ISystemAccountRepository, Repositories.Repositories.SystemAccountRepository>();

builder.Services.AddScoped<DataAccess.DataAccessLayer.TagDAO>();
builder.Services.AddScoped<Repositories.Repositories.ITagRepository, Repositories.Repositories.TagRepository>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
