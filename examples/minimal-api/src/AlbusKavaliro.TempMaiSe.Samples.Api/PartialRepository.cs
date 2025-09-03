using Microsoft.EntityFrameworkCore;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Samples.Api;

public class PartialRepository : IPartialRepository
{
    private readonly TemplateContext _context;

    public PartialRepository(TemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public Task<Partial?> GetPartialAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return GetPartialImplAsync(key, cancellationToken);
    }

    private async Task<Partial?> GetPartialImplAsync(string key, CancellationToken cancellationToken)
        => await _context.Partials.SingleOrDefaultAsync(partial => partial.Key == key, cancellationToken).ConfigureAwait(false);
}
