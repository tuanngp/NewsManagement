using BusinessObjects.Models;
using Repositories;
using Repositories.RepositoryImpl;

namespace Services.ServiceImpl;

public class TagService : ITagService
{
    private ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
    }

    public Task<IEnumerable<Tag>> GetAllAsync()
    {
        return _tagRepository.GetAllAsync();
    }

    public Task<Tag?> GetByIdAsync(int id)
    {
        return _tagRepository.GetByIdAsync(id);
    }

    public Task<Tag> AddAsync(Tag entity)
    {
        return _tagRepository.AddAsync(entity);
    }

    public Task<Tag> UpdateAsync(Tag entity)
    {
        return _tagRepository.UpdateAsync(entity);
    }

    public Task DeleteAsync(int id)
    {
        return _tagRepository.DeleteAsync(id);
    }
}