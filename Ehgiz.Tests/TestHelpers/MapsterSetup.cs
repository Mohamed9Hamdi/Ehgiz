using System.Runtime.CompilerServices;
using Mapster;

namespace Ehgiz.Tests.TestHelpers;

internal static class MapsterSetup
{
    [ModuleInitializer]
    public static void Init()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(Ehgiz.Application.DependencyInjection).Assembly);
    }
}
