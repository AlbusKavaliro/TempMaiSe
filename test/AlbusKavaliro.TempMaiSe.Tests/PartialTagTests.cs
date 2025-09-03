using System;
using System.Text.Encodings.Web;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class PartialTagTests
{
    [Fact]
    public async Task WriteToAsync_Completes_Normally_If_No_Partial_Is_Given()
    {
        // Arrange
        LiteralExpression value = new(new StringValue(""));
        await using StringWriter writer = new();

        // Act
        Completion result = await PartialTag.WriteToAsync(value, writer, NullEncoder.Default, new TemplateContext());

        // Assert
        Assert.Equal(Completion.Normal, result);
    }

    [Fact]
    public async Task WriteToAsync_Completes_Normally_If_No_ServiceProvider_Is_Given()
    {
        // Arrange
        LiteralExpression value = new(new StringValue(""));
        await using StringWriter writer = new();
        TemplateContext context = new();

        // Act
        Completion result = await PartialTag.WriteToAsync(value, writer, NullEncoder.Default, context);

        // Assert
        Assert.Equal(Completion.Normal, result);
    }

    [Fact]
    public async Task WriteToAsync_Throws_If_Partial_Is_Not_Found()
    {
        // Arrange
        LiteralExpression value = new(new StringValue("I_DONT_EXIST"));
        await using StringWriter writer = new();
        Mock<IPartialRepository> partialRepository = new();
        Mock<IServiceProvider> serviceProvider = new();
        serviceProvider.Setup(s => s.GetService(typeof(IPartialRepository))).Returns(partialRepository.Object);
        TemplateContext context = new();
        context.AmbientValues.Add(Constants.ServiceProvider, serviceProvider.Object);

        // Act/Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            _ = await PartialTag.WriteToAsync(value, writer, NullEncoder.Default, context).ConfigureAwait(false);
        }).ConfigureAwait(true);
    }
}
