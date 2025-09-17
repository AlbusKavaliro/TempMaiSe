using FluentEmail.Core;
using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class MailInformationToMailMapperTests
{
    private readonly MailInformationToMailMapper _mapper = new();

    [Fact]
    public void Map_Returns_Original_Email_Instance()
    {
        // Arrange
        MailInformation info = new() { To = { "dummy@example.org" } };
        Email email = new();

        // Act
        IFluentEmail actual = _mapper.Map(info, email);

        // Assert
        Assert.Same(actual, email);
    }

    [Fact]
    public void Map_Throws_ArgumentNullException_When_Given_No_Information()
    {
        // Arrange
        IFluentEmail email = new Mock<IFluentEmail>().Object;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>("mailInformation", () => _mapper.Map(null, email));
    }

    [Fact]
    public void Map_Throws_ArgumentNullException_When_Given_No_Email()
    {
        // Arrange
        MailInformation info = new();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>("email", () => _mapper.Map(info, null));
    }

    [Theory]
    [InlineData("foo@example.org")]
    [InlineData("foo@example.net")]
    public void Map_Sets_From_From_Information(string address)
    {
        // Arrange
        MailInformation info = new() { From = address };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.SetFrom(address, null)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org")]
    [InlineData("foo@example.net")]
    public void Map_Adds_To_From_Information(string address)
    {
        // Arrange
        MailInformation info = new() { To = { address } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.To(address)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org")]
    [InlineData("foo@example.net")]
    public void Map_Adds_Cc_From_Information(string address)
    {
        // Arrange
        MailInformation info = new() { Cc = { address } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.CC(address, string.Empty)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org")]
    [InlineData("foo@example.net")]
    public void Map_Adds_Bcc_From_Information(string address)
    {
        // Arrange
        MailInformation info = new() { Bcc = { address } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.BCC(address, string.Empty)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("foo@example.org")]
    [InlineData("foo@example.net")]
    public void Map_Adds_ReplyTo_From_Information(string address)
    {
        // Arrange
        MailInformation info = new() { ReplyTo = { address } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.ReplyTo(address)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test")]
    [InlineData("some_other_value")]
    public void Map_Adds_Single_Tag_From_Information(string someTagName)
    {
        // Arrange
        MailInformation info = new() { Tags = { someTagName } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Tag(someTagName)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test", "whatever")]
    [InlineData("some_other_value", "example")]
    public void Map_Adds_Multiple_Tags_From_Information(string someTagName, string someOtherTagName)
    {
        // Arrange
        MailInformation info = new() { Tags = { someTagName, someOtherTagName } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Tag(someTagName)).Returns(emailMock.Object).Verifiable();
        emailMock.Setup(it => it.Tag(someOtherTagName)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test", "it")]
    [InlineData("some_other_value", "is also cool")]
    public void Map_Adds_Single_Header_From_Information(string headerName, string headerValue)
    {
        // Arrange
        MailInformation info = new() { Headers = { new() { Name = headerName, Value = headerValue } } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Header(headerName, headerValue)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test", "whatever", "do", "your thing")]
    [InlineData("some_other_value", "example", "x-dont-spam-me", "some score")]
    public void Map_Adds_Multiple_Headers_From_Information(string firstHeaderName, string firstHeaderValue, string secondHeaderName, string secondHeaderValue)
    {
        // Arrange
        MailInformation info = new() { Headers = { new() { Name = firstHeaderName, Value = firstHeaderValue }, new() { Name = secondHeaderName, Value = secondHeaderValue } } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Header(firstHeaderName, firstHeaderValue)).Returns(emailMock.Object).Verifiable();
        emailMock.Setup(it => it.Header(secondHeaderName, secondHeaderValue)).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Map_Sets_LowPriority_From_Information()
    {
        // Arrange
        MailInformation info = new() { Priority = Priority.Low };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.LowPriority()).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Map_Sets_HighPriority_From_Information()
    {
        // Arrange
        MailInformation info = new() { Priority = Priority.High };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.HighPriority()).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(info, emailMock.Object);

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
        MailInformation template = new() { Attachments = { attachment } };
        Mock<IFluentEmail> emailMock = new();
        emailMock.Setup(it => it.Attach(It.Is<FluentEmail.Core.Models.Attachment>(a => a.Filename == attachment.FileName && !a.IsInline && a.ContentType == attachment.MediaType && a.Data.EqualsBuffer(attachment.Data)))).Returns(emailMock.Object).Verifiable();

        // Act
        _ = _mapper.Map(template, emailMock.Object);

        // Assert
        emailMock.VerifyAll();
        emailMock.VerifyNoOtherCalls();
    }
}
