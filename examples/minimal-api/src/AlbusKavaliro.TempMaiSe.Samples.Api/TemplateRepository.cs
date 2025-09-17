
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Samples.Api;

public class TemplateRepository : ITemplateRepository
{
    private readonly TemplateContext _context;

    public TemplateRepository(TemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Template?> GetTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Templates.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
    }
}
