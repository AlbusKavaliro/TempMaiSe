namespace AlbusKavaliro.TempMaiSe.Models;

public interface IPartialRepository
{
    /// <summary>
    /// Retrieves a partial asynchronously based on the specified key.
    /// </summary>
    /// <param name="key">The key of the partial to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The retrieved partial, or null if not found.</returns>
    Task<Partial?> GetPartialAsync(string key, CancellationToken cancellationToken = default);
}
