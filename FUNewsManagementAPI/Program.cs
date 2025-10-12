using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OData;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RoleSettings>(builder.Configuration.GetSection("RoleSettings"));

builder.Services.AddDbContext<FunewsManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FUNewsManagementDB"))); // FIXED: use correct key

builder.Services.AddScoped<DataAccess.DataAccessLayer.CategoryDAO>();
builder.Services.AddScoped<Repositories.Repositories.ICategoryRepository, Repositories.Repositories.CategoryRepository>();

builder.Services.AddScoped<DataAccess.DataAccessLayer.NewsArticleDAO>();
builder.Services.AddScoped<Repositories.Repositories.INewsArticleRepository, Repositories.Repositories.NewsArticleRepository>();

builder.Services.AddScoped<DataAccess.DataAccessLayer.SystemAccountDAO>();
builder.Services.AddScoped<Repositories.Repositories.ISystemAccountRepository, Repositories.Repositories.SystemAccountRepository>();

builder.Services.AddScoped<DataAccess.DataAccessLayer.TagDAO>();
builder.Services.AddScoped<Repositories.Repositories.ITagRepository, Repositories.Repositories.TagRepository>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.MaxDepth = 256;
    })
    .AddOData(opt =>
    {
        opt.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
    });

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Configuration["Authentication:Cookie:CookieName"] ?? "FUNews.Auth";
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public sealed class RoleSettings
{
    public int AdminRoleValue { get; set; } = 3;
}