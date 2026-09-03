using System.Diagnostics;
using Fluid;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Newtonsoft.Json.Schema;

using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

using OneOf;
using OneOf.Types;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class MailServiceTests
{
    [Fact]
    public async Task SendMailAsync_TemplateNotFound_ReturnsNotFound()
    {
        // Arrange
        Mock<IFluentEmailFactory> mailFactory = new();
        Mock<ITemplateRepository> templateRepository = new();
        Mock<IDataParser> dataParser = new();
        FluidParser fluidParser = new();
        Mock<ITemplateToMailMapper> mailHeaderMapper = new();
        Mock<IMailInformationToMailMapper> mailInfoMapper = new();
        Mock<IServiceProvider> serviceProvider = new();
        Mock<IFeatureManager> featureManager = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        int templateId = 1;
        templateRepository.Setup(c => c.GetTemplateAsync(templateId, It.IsAny<CancellationToken>())).Returns(Task.FromResult<Template?>(null));

        Stream data = new MemoryStream();

        MailService mailService = new(
            mailFactory.Object,
            templateRepository.Object,
            dataParser.Object,
            fluidParser,
            mailHeaderMapper.Object,
            mailInfoMapper.Object,
            serviceProvider.Object,
            featureManager.Object,
            configuration
        );

        // Act
        OneOf<SendResponse, NotFound, List<ValidationError>> result = await mailService.SendMailAsync(templateId, data, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.IsType<NotFound>(result.Value);

        // Verify
        templateRepository.Verify();
    }

    [Fact]
    public async Task SendMailAsync_Injects_Traceparent_Header_When_Feature_Is_Enabled()
    {
        // Arrange
        Mock<IFluentEmail> mail = new();
        mail.Setup(m => m.Header(It.IsAny<string>(), It.IsAny<string>())).Returns(mail.Object);
        mail.Setup(m => m.Subject(It.IsAny<string>())).Returns(mail.Object);
        mail.Setup(m => m.SendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SendResponse());

        Mock<IFeatureManager> featureManager = new();
        featureManager.Setup(m => m.IsEnabledAsync("TempMaiSe.InjectTraceHeaders")).ReturnsAsync(true);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureManagement:TempMaiSe.InjectTraceHeaders"] = "true" })
            .Build();

        MailService mailService = CreateMailService(mail, featureManager, configuration);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", activity.Id!), Times.Once);
    }

    [Fact]
    public async Task SendMailAsync_Does_Not_Inject_Traceparent_Header_When_Feature_Is_Disabled()
    {
        // Arrange
        Mock<IFluentEmail> mail = new();
        mail.Setup(m => m.Header(It.IsAny<string>(), It.IsAny<string>())).Returns(mail.Object);
        mail.Setup(m => m.Subject(It.IsAny<string>())).Returns(mail.Object);
        mail.Setup(m => m.SendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SendResponse());

        Mock<IFeatureManager> featureManager = new();
        featureManager.Setup(m => m.IsEnabledAsync("TempMaiSe.InjectTraceHeaders")).ReturnsAsync(false);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureManagement:TempMaiSe.InjectTraceHeaders"] = "false" })
            .Build();

        MailService mailService = CreateMailService(mail, featureManager, configuration);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", It.IsAny<string>()), Times.Never);
    }

    private static MailService CreateMailService(Mock<IFluentEmail> mail, Mock<IFeatureManager> featureManager, IConfiguration configuration)
    {
        Mock<IFluentEmailFactory> mailFactory = new();
        mailFactory.Setup(m => m.Create()).Returns(mail.Object);

        Mock<ITemplateRepository> templateRepository = new();
        templateRepository
            .Setup(m => m.GetTemplateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Template
            {
                Id = 1,
                Data = new TemplateData
                {
                    SubjectTemplate = "Hello",
                    JsonSchema = "{}"
                }
            });

        Mock<IDataParser> dataParser = new();
        dataParser
            .Setup(m => m.ParseAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MailInformation { Data = new { Name = "Tester" } });

        Mock<ITemplateToMailMapper> mailHeaderMapper = new();
        mailHeaderMapper.Setup(m => m.Map(It.IsAny<TemplateData>(), It.IsAny<IFluentEmail>())).Returns(mail.Object);

        Mock<IMailInformationToMailMapper> mailInfoMapper = new();
        mailInfoMapper.Setup(m => m.Map(It.IsAny<MailInformation>(), It.IsAny<IFluentEmail>())).Returns(mail.Object);

        Mock<IServiceProvider> serviceProvider = new();

        return new MailService(
            mailFactory.Object,
            templateRepository.Object,
            dataParser.Object,
            new FluidParser(),
            mailHeaderMapper.Object,
            mailInfoMapper.Object,
            serviceProvider.Object,
            featureManager.Object,
            configuration);
    }
}