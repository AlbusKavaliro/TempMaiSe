using Newtonsoft.Json.Schema;
using FluentEmail.Core.Models;

using OneOf;
using OneOf.Types;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Represents a service for sending emails.
/// </summary>
public interface IMailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="id">The ID of the template to use for the email.</param>
    /// <param name="data">The data stream containing the email information in JSON format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains one of the following:
    /// - SendResponse: The response indicating the email was sent successfully.
    /// - NotFound: The template with the specified ID was not found.
    /// - List&lt;ValidationError&gt;: A list of validation errors if the email data is invalid.
    /// </returns>
    Task<OneOf<SendResponse, NotFound, List<ValidationError>>> SendMailAsync(int id, Stream data, CancellationToken cancellationToken = default);
}
