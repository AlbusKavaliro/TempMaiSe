using Fluid;
using Newtonsoft.Json.Schema;
using FluentEmail.Core;
using FluentEmail.Core.Models;

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
        Mock<DataParser> dataParser = new();
        Mock<FluidParser> fluidParser = new();
        Mock<ITemplateToMailMapper> mailHeaderMapper = new();
        Mock<IMailInformationToMailMapper> mailInfoMapper = new();
        Mock<IServiceProvider> serviceProvider = new();

        int templateId = 1;
        templateRepository.Setup(c => c.GetTemplateAsync(templateId, It.IsAny<CancellationToken>())).Returns(Task.FromResult<Template?>(null));

        Stream data = new MemoryStream();

        MailService mailService = new(
            mailFactory.Object,
            templateRepository.Object,
            dataParser.Object,
            fluidParser.Object,
            mailHeaderMapper.Object,
            mailInfoMapper.Object,
            serviceProvider.Object
        );

        // Act
        OneOf<SendResponse, NotFound, List<ValidationError>> result = await mailService.SendMailAsync(templateId, data).ConfigureAwait(true);

        // Assert
        Assert.IsType<NotFound>(result.Value);

        // Verify
        templateRepository.Verify();
    }
}
