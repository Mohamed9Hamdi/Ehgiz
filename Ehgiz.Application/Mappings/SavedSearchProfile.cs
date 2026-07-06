using Ehgiz.Application.DTOs.SavedSearches;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class SavedSearchProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SavedSearch, SavedSearchDto>()
            .Map(dest => dest.CategoryName, src => src.Category == null ? null : src.Category.Name)
            .Map(dest => dest.Condition, src => src.Condition == null ? null : src.Condition.Value.ToString());
    }
}
