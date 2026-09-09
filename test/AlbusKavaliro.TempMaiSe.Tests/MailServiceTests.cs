using System.Diagnostics;
using Fluid;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Newtonsoft.Json.Schema;

using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class MailServiceTests
{
    private const string TraceHeadersFeatureName = "TempMaiSe.InjectTraceHeaders";

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
        SendMailResult result = await mailService.SendMailAsync(templateId, data, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.IsType<NotFound>(result.Value);

        // Verify
        templateRepository.Verify();
    }

    [Fact]
    public async Task SendMailAsync_Injects_Traceparent_Header_When_Feature_Is_Enabled()
    {
        // Arrange
        Mock<IFluentEmail> mail = CreateMailMock();

        Mock<IFeatureManager> featureManager = new();
        featureManager.Setup(m => m.IsEnabledAsync(TraceHeadersFeatureName)).ReturnsAsync(true);

        IConfiguration configuration = CreateFeatureConfiguration(true);

        MailService mailService = CreateMailService(mail, featureManager, configuration);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", activity.Id!), Times.Once);
        featureManager.Verify(m => m.IsEnabledAsync(TraceHeadersFeatureName), Times.Once);
    }

    [Fact]
    public async Task SendMailAsync_Does_Not_Inject_Traceparent_Header_When_Feature_Is_Disabled()
    {
        // Arrange
        Mock<IFluentEmail> mail = CreateMailMock();

        Mock<IFeatureManager> featureManager = new();
        featureManager.Setup(m => m.IsEnabledAsync(TraceHeadersFeatureName)).ReturnsAsync(false);

        IConfiguration configuration = CreateFeatureConfiguration(false);

        MailService mailService = CreateMailService(mail, featureManager, configuration);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", It.IsAny<string>()), Times.Never);
        featureManager.Verify(m => m.IsEnabledAsync(TraceHeadersFeatureName), Times.Once);
    }

    [Fact]
    public async Task SendMailAsync_Injects_Traceparent_Header_When_Feature_Is_Not_Configured()
    {
        // Arrange
        Mock<IFluentEmail> mail = CreateMailMock();
        Mock<IFeatureManager> featureManager = new(MockBehavior.Strict);
        IConfiguration configuration = new ConfigurationBuilder().Build();
        MailService mailService = CreateMailService(mail, featureManager, configuration);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", activity.Id!), Times.Once);
        featureManager.Verify(m => m.IsEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendMailAsync_Injects_Tracestate_Header_When_Activity_Has_TraceState_And_Feature_Is_Enabled()
    {
        // Arrange
        Mock<IFluentEmail> mail = CreateMailMock();

        Mock<IFeatureManager> featureManager = new();
        featureManager.Setup(m => m.IsEnabledAsync(TraceHeadersFeatureName)).ReturnsAsync(true);

        IConfiguration configuration = CreateFeatureConfiguration(true);
        MailService mailService = CreateMailService(mail, featureManager, configuration);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.TraceStateString = "vendor=value";

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", activity.Id!), Times.Once);
        mail.Verify(m => m.Header("tracestate", "vendor=value"), Times.Once);
    }

    [Fact]
    public async Task SendMailAsync_Without_FeatureManager_And_Configuration_Defaults_To_Injecting_Trace_Headers()
    {
        // Arrange
        Mock<IFluentEmail> mail = CreateMailMock();
        MailService mailService = CreateMailService(mail);

        using Activity activity = new("send-mail");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Act
        _ = await mailService.SendMailAsync(1, new MemoryStream(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        mail.Verify(m => m.Header("traceparent", activity.Id!), Times.Once);
    }

    private static IConfiguration CreateFeatureConfiguration(bool enabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"FeatureManagement:{TraceHeadersFeatureName}"] = enabled ? "true" : "false" })
            .Build();
    }

    private static Mock<IFluentEmail> CreateMailMock()
    {
        Mock<IFluentEmail> mail = new();
        mail.Setup(m => m.Header(It.IsAny<string>(), It.IsAny<string>())).Returns(mail.Object);
        mail.Setup(m => m.Subject(It.IsAny<string>())).Returns(mail.Object);
        mail.Setup(m => m.SendAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SendResponse());
        return mail;
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

    private static MailService CreateMailService(Mock<IFluentEmail> mail)
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
            serviceProvider.Object);
    }
}