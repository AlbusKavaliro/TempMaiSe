namespace AlbusKavaliro.TempMaiSe.Models;

public interface ITemplateRepository
{
    /// <summary>
    /// Retrieves a template asynchronously based on the specified ID.
    /// </summary>
    /// <param name="id">The ID of the template to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The retrieved template, or null if not found.</returns>
    Task<Template?> GetTemplateAsync(int id, CancellationToken cancellationToken = default);
}
