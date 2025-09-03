using FluentEmail.Core;
using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class TemplateToMailMapperTests
{
    private readonly TemplateToMailMapper _mapper = new();

    [Fact]
    public void Map_Returns_Original_Email_Instance()
    {
        // Arrange
        TemplateData template = new() { To = { new() { Address = "dummy@example.org" } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Email email = new();

        // Act
        IFluentEmail actual = _mapper.Map(template, email);

        // Assert
        Assert.Same(actual, email);
    }

    [Fact]
    public void Map_Throws_ArgumentNullException_When_Given_No_Template()
    {
        // Arrange
        IFluentEmail email = new Mock<IFluentEmail>().Object;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>("configuredTemplate", () => _mapper.Map(null, email));
    }

    [Fact]
    public void Map_Throws_ArgumentNullException_When_Given_No_Email()
    {
        // Arrange
        TemplateData template = new() { To = { new() { Address = "dummy@example.org", Name = "Foo Bar" } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>("email", () => _mapper.Map(template, null));
    }

    [Theory]
    [InlineData("foo@example.org", null)]
    [InlineData("foo@example.net", "dummy")]
    public void Map_Sets_From_From_Template(string address, string? name)
    {
        // Arrange
        TemplateData template = new() { From = new() { Address = address, Name = name }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.SetFrom(address, name)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org", null)]
    [InlineData("foo@example.net", "dummy")]
    public void Map_Adds_To_From_Template(string address, string? name)
    {
        // Arrange
        TemplateData template = new() { To = { new() { Address = address, Name = name } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.To(address, name)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org", null)]
    [InlineData("foo@example.net", "dummy")]
    public void Map_Adds_Cc_From_Template(string address, string? name)
    {
        // Arrange
        TemplateData template = new() { Cc = { new() { Address = address, Name = name } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.CC(address, name)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org", null)]
    [InlineData("foo@example.net", "dummy")]
    public void Map_Adds_Bcc_From_Template(string address, string? name)
    {
        // Arrange
        TemplateData template = new() { Bcc = { new() { Address = address, Name = name } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.BCC(address, name)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org", null)]
    [InlineData("foo@example.net", "dummy")]
    public void Map_Adds_ReplyTo_From_Template(string address, string? name)
    {
        // Arrange
        TemplateData template = new() { ReplyTo = { new() { Address = address, Name = name } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.ReplyTo(address, name)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test")]
    [InlineData("some_other_value")]
    public void Map_Adds_Single_Tag_From_Template(string someTagName)
    {
        // Arrange
        TemplateData template = new() { Tags = { new() { Name = someTagName } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Tag(someTagName)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test", "whatever")]
    [InlineData("some_other_value", "example")]
    public void Map_Adds_Multiple_Tags_From_Template(string someTagName, string someOtherTagName)
    {
        // Arrange
        TemplateData template = new() { Tags = { new() { Name = someTagName }, new() { Name = someOtherTagName } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Tag(someTagName)).Returns(emailMock.Object).Verifiable();
        emailMock.Setup(it => it.Tag(someOtherTagName)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test", "it")]
    [InlineData("some_other_value", "is also cool")]
    public void Map_Adds_Single_Header_From_Template(string headerName, string headerValue)
    {
        // Arrange
        TemplateData template = new() { Headers = { new Header { Name = headerName, Value = headerValue } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Header(headerName, headerValue)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test", "whatever", "do", "your thing")]
    [InlineData("some_other_value", "example", "x-dont-spam-me", "some score")]
    public void Map_Adds_Multiple_Headers_From_Template(string firstHeaderName, string firstHeaderValue, string secondHeaderName, string secondHeaderValue)
    {
        // Arrange
        TemplateData template = new() { Headers = { new Header { Name = firstHeaderName, Value = firstHeaderValue }, new Header { Name = secondHeaderName, Value = secondHeaderValue } }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Header(firstHeaderName, firstHeaderValue)).Returns(emailMock.Object).Verifiable();
        emailMock.Setup(it => it.Header(secondHeaderName, secondHeaderValue)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Map_Sets_LowPriority_From_Template()
    {
        // Arrange
        TemplateData template = new() { Priority = Priority.Low, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.LowPriority()).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Map_Sets_HighPriority_From_Template()
    {
        // Arrange
        TemplateData template = new() { Priority = Priority.High, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.HighPriority()).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Map_Adds_Attachment_From_Template()
    {
        // Arrange
        byte[] data = [0x00, 0x01, 0x02, 0x03];
        Attachment attachment = new() { FileName = "dummy.txt", MediaType = "text/plain", Data = data };
        TemplateData template = new() { Attachments = { attachment }, SubjectTemplate = "Dummy", HtmlBodyTemplate = "Dummy", JsonSchema = "{}" };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Attach(It.Is<FluentEmail.Core.Models.Attachment>(a => a.Filename == attachment.FileName && !a.IsInline && a.ContentType == attachment.MediaType && a.Data.EqualsBuffer(attachment.Data)))).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }
}
