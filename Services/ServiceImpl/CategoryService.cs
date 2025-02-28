using BusinessObjects.Models;
using Repositories;
using Repositories.RepositoryImpl;

namespace Services.ServiceImpl;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public Task<IEnumerable<Category>> GetAllAsync()
    {
        return _categoryRepository.GetAllAsync();
    }

    public Task<Category?> GetByIdAsync(short id)
    {
        return _categoryRepository.GetByIdAsync(id);
    }

    public Task<Category> AddAsync(Category entity)
    {
        return _categoryRepository.AddAsync(entity);
    }

    public Task<Category> UpdateAsync(Category entity)
    {
        return _categoryRepository.UpdateAsync(entity);
    }

    public Task DeleteAsync(short id)
    {
        return _categoryRepository.DeleteAsync(id);
    }
}