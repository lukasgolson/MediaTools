using Microsoft.Extensions.DependencyInjection;
using TreeBasedCli;
namespace Extractor;

public sealed class DependencyInjectionService : IDependencyInjectionService
{
    private static readonly Lazy<DependencyInjectionService> Lazy = new(() => new DependencyInjectionService());
    private readonly IServiceProvider _serviceProvider;

    private DependencyInjectionService()
    {
        var services = BuildServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    public static DependencyInjectionService Instance => Lazy.Value;

    public T Resolve<T>() where T : notnull
    {
        return GetUnregisteredService<T>();
    }

    private static ServiceCollection BuildServiceCollection()
    {
        var services = new ServiceCollection();

        // nothing here

        return services;
    }

    public T GetUnregisteredService<T>() where T : notnull
    {
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider);
    }
}
