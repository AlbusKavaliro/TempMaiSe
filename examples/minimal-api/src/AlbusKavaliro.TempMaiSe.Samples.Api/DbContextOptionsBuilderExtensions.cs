using Microsoft.EntityFrameworkCore;

namespace AlbusKavaliro.TempMaiSe.Samples.Api;

public static class DbContextOptionsBuilderExtensions
{
    public static IServiceCollection AddTemplateContext(this IServiceCollection services, ConfigurationManager config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        return services.AddDbContext<TemplateContext>(options =>
        {
            options.UseInMemoryDatabase(nameof(TempMaiSe));
        });
    }
}
