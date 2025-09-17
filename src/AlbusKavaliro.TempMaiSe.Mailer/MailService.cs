using Fluid;
using Newtonsoft.Json.Schema;
using FluentEmail.Core;
using FluentEmail.Core.Models;

using System.Diagnostics;
using System.Text.Encodings.Web;

using AlbusKavaliro.TempMaiSe.Models;

using OneOf;
using OneOf.Types;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Represents a service for sending emails.
/// </summary>
public class MailService : IMailService
{
    private readonly IFluentEmailFactory _mailFactory;

    private readonly ITemplateRepository _templateRepository;

    private readonly IDataParser _dataParser;

    private readonly FluidParser _fluidParser;

    private readonly ITemplateToMailMapper _mailHeaderMapper;

    private readonly IMailInformationToMailMapper _mailInfoMapper;

    private readonly IServiceProvider _serviceProvider;

    public MailService(
        IFluentEmailFactory mailFactory,
        ITemplateRepository templateRepository,
        IDataParser dataParser,
        FluidParser fluidParser,
        ITemplateToMailMapper mailHeaderMapper,
        IMailInformationToMailMapper mailInfoMapper,
        IServiceProvider serviceProvider)
    {
        _mailFactory = mailFactory ?? throw new ArgumentNullException(nameof(mailFactory));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
        _fluidParser = fluidParser ?? throw new ArgumentNullException(nameof(fluidParser));
        _mailHeaderMapper = mailHeaderMapper ?? throw new ArgumentNullException(nameof(mailHeaderMapper));
        _mailInfoMapper = mailInfoMapper ?? throw new ArgumentNullException(nameof(mailInfoMapper));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public async Task<OneOf<SendResponse, NotFound, List<ValidationError>>> SendMailAsync(int id, Stream data, CancellationToken cancellationToken = default)
    {
        using Activity? activity = MailingInstrumentation.Instance?.ActivitySource.StartActivity("SendMail")!;
        activity?.AddTag("TemplateId", id);

        Template? template = await _templateRepository.GetTemplateAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return new NotFound();
        }

        TemplateData templateData = template.Data;

        OneOf<MailInformation, List<ValidationError>> mailInformationOrErrors = await _dataParser.ParseAsync(templateData.JsonSchema, data, cancellationToken).ConfigureAwait(false);
        if (mailInformationOrErrors.TryPickT1(out List<ValidationError> errors, out MailInformation? mailInformation))
        {
            return errors;
        }

        InlineAttachmentCollection inlineAttachments = MergeInlineAttachments(templateData, mailInformation);

        IFluentEmail mail = _mailFactory.Create();
        mail = _mailHeaderMapper.Map(templateData, mail);
        mail = _mailInfoMapper.Map(mailInformation, mail);

        IFluidTemplate fluidSubjectTemplate = _fluidParser.Parse(templateData.SubjectTemplate);
        TemplateContext templateContext = new(mailInformation.Data)
        {
            AmbientValues =
            {
                { nameof(InlineAttachmentCollection), inlineAttachments },
                { Constants.Extensibility, new FluidExtensibility(inlineAttachments) },
                { Constants.ServiceProvider, _serviceProvider }
            }
        };
        string subject = await fluidSubjectTemplate.RenderAsync(templateContext).ConfigureAwait(false);
        mail = mail.Subject(subject);
        mail = await RenderBodiesAsync(mail, templateData, templateContext).ConfigureAwait(false);
        mail = AttachInlineAttachments(mail, inlineAttachments);

        SendResponse resp = await mail.SendAsync(cancellationToken).ConfigureAwait(false);
        MailingInstrumentation.Instance?.MailsSent.Add(1);
        return resp;
    }

    private static InlineAttachmentCollection MergeInlineAttachments(TemplateData templateData, MailInformation mailInformation)
    {
        InlineAttachmentCollection inlineAttachments = new(templateData.InlineAttachments?.Count ?? 0 + mailInformation.InlineAttachments?.Count ?? 0);
        templateData.InlineAttachments?.CopyTo(inlineAttachments);
        mailInformation.InlineAttachments?.CopyTo(inlineAttachments);
        return inlineAttachments;
    }

    private static IFluentEmail AttachInlineAttachments(IFluentEmail mail, InlineAttachmentCollection inlineAttachments)
    {
        foreach (InlineAttachmentWithId attachmentWithId in inlineAttachments)
        {
            Models.Attachment attachment = attachmentWithId.Attachment;
            FluentEmail.Core.Models.Attachment fluentAttachment = new() { ContentId = attachmentWithId.Id, Filename = attachment.FileName, ContentType = attachment.MediaType, Data = new MemoryStream(attachment.Data), IsInline = true };
            mail = mail.Attach(fluentAttachment);
        }

        return mail;
    }

    private async Task<IFluentEmail> RenderBodiesAsync(IFluentEmail mail, TemplateData templateData, TemplateContext templateContext)
    {
        string? plainTextBody = null;
        if (!string.IsNullOrWhiteSpace(templateData.PlainTextBodyTemplate))
        {
            IFluidTemplate plainTextFluidTemplate = _fluidParser.Parse(templateData.PlainTextBodyTemplate);
            templateContext.EnterChildScope();
            try
            {
                _ = templateContext.SetValue(Constants.IsHtml, false);
                plainTextBody = await plainTextFluidTemplate.RenderAsync(templateContext).ConfigureAwait(false);
            }
            finally
            {
                templateContext.ReleaseScope();
            }
        }

        string? htmlBody = null;
        if (!string.IsNullOrWhiteSpace(templateData.HtmlBodyTemplate))
        {
            IFluidTemplate htmlFluidTemplate = _fluidParser.Parse(templateData.HtmlBodyTemplate);
            templateContext.EnterChildScope();
            try
            {
                _ = templateContext.SetValue(Constants.IsHtml, true);
                htmlBody = await htmlFluidTemplate.RenderAsync(templateContext, HtmlEncoder.Default).ConfigureAwait(false);
            }
            finally
            {
                templateContext.ReleaseScope();
            }
        }

        if (!string.IsNullOrWhiteSpace(htmlBody) && !string.IsNullOrWhiteSpace(plainTextBody))
        {
            mail = mail.Body(htmlBody, true)
                .PlaintextAlternativeBody(plainTextBody);
        }
        else if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            mail = mail.Body(htmlBody, true);
        }
        else if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            mail = mail.Body(plainTextBody);
        }

        return mail;
    }
}
