using Ehgiz.Application.DTOs.Admin;
using Ehgiz.DAL.Entities;
using Mapster;

namespace Ehgiz.Application.Mappings;

public class IssueReportProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IssueReport, IssueReportDto>()
            .Map(dest => dest.ReporterName, src => src.Reporter.FullName)
            .Map(dest => dest.Status, src => src.Status == null ? string.Empty : src.Status.ToString());
    }
}
