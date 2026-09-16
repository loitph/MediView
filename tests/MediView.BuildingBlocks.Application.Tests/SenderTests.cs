using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.BuildingBlocks.Application.Tests;

internal sealed record SayHello(string Name) : ICommand<Result<string>>;

internal sealed class SayHelloHandler : ICommandHandler<SayHello, Result<string>>
{
    public Task<Result<string>> Handle(SayHello request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Task.FromResult(Result.Failure<string>(new Error("hello.empty_name", "Name is required.")));
        }

        Result<string> greeting = $"Hello, {request.Name}!";
        return Task.FromResult(greeting);
    }
}

public sealed class SenderTests
{
    // Build a real DI container, exactly like Program.cs would, and hand back the ISender.
    private static ISender CreateSender()
    {
        var provider = new ServiceCollection()
            .AddApplication(typeof(SayHelloHandler).Assembly)
            .BuildServiceProvider();

        return provider.CreateScope().ServiceProvider.GetRequiredService<ISender>();
    }

    [Fact]
    public async Task SendReturnsSuccessFromHandler()
    {
        // Arrange
        var sender = CreateSender();

        // Act
        var result = await sender.Send(new SayHello("Loi"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello, Loi!", result.Value);
    }

    [Fact]
    public async Task SendReturnsFailureFromHandler()
    {
        var sender = CreateSender();

        var result = await sender.Send(new SayHello(""), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("hello.empty_name", result.Error.Code);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public async Task SendThrowsWhenNoHandlerIsRegistered()
    {
        var sender = new ServiceCollection()
            .AddApplication(typeof(ISender).Assembly)
            .BuildServiceProvider()
            .GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new SayHello("Loi"), TestContext.Current.CancellationToken));
    }
}
