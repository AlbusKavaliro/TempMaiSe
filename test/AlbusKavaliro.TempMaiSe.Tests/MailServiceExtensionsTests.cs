using Fluid;
using AlbusKavaliro.TempMaiSe.Mailer;

namespace AlbusKavaliro.TempMaiSe.Tests;

public class MailServiceExtensionsTests
{
    [Fact]
    public void AddMailService_Throws_ArgumentNullException_When_Given_Null_ServiceCollection()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>("services", () => MailServiceExtensions.AddMailService(services!));
    }

    [Fact]
    public void AddMailService_Can_Be_Called_Without_Parser_Configuration()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        MailServiceExtensions.AddMailService(services);

        // Assert
        Assert.Single(services, sr => sr.ServiceType.Equals(typeof(FluidMailParser)));
        Assert.Single(services, sr => sr.ServiceType.Equals(typeof(FluidParser)));
    }

    [Fact]
    public void AddMailService_Calls_Given_Action_To_Configure_Parser()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        bool called = false;
        void configureParser(IServiceProvider _, FluidParser __) => called = true;

        // Act
        MailServiceExtensions.AddMailService(services, configureParser);

        // Assert
        _ = services.BuildServiceProvider().GetService<FluidParser>(); // Ensure parser is created
        Assert.Single(services, sr => sr.ServiceType.Equals(typeof(FluidMailParser)));
        Assert.Single(services, sr => sr.ServiceType.Equals(typeof(FluidParser)));
        Assert.True(called);
    }

    [Fact]
    public void AddMailService_Can_Be_Called_Without_Parser_Configuration_With_FluidParserOptions()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        MailServiceExtensions.AddMailService(services, options: new FluidParserOptions { AllowFunctions = true, AllowParentheses = true });

        // Assert
        Assert.Single(services, sr => sr.ServiceType.Equals(typeof(FluidMailParser)));
        Assert.Single(services, sr => sr.ServiceType.Equals(typeof(FluidParser)));
    }

    [Fact]
    public void AddMailService_Calls_Action_To_Configure_Parser()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        bool called = false;

        // Act
        MailServiceExtensions.AddMailService(services, (sp, parser) => called = true);
        _ = services.BuildServiceProvider().GetRequiredService<FluidParser>();

        // Assert
        Assert.True(called);
    }
}
