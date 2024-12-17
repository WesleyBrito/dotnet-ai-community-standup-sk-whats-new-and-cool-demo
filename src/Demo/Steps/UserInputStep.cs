#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;

namespace Demo.Steps;

internal sealed class UserInputStep : KernelProcessStep<UserInputState>
{
    private UserInputState state;

    public static class Functions
    {
        public const string GetUserInput = nameof(GetUserInput);

        public const string GetUserConfirmation = nameof(GetUserConfirmation);
    }

    public override ValueTask ActivateAsync(KernelProcessStepState<UserInputState> state)
    {
        this.state = state.State!;

        return ValueTask.CompletedTask;
    }

    [KernelFunction(Functions.GetUserInput)]
    public async ValueTask GetUserInputAsync(KernelProcessStepContext context)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nPlease enter a marketing request, or say «bye» to exit: ");
        Console.ForegroundColor = ConsoleColor.White;
        var userMessage = Console.ReadLine();

        if (userMessage?.StartsWith(@"bye", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            await context.EmitEventAsync(new() { Id = Events.Exit, Data = userMessage });
            return;
        }

        state.UserInputs.Add(userMessage!);
        state.CurrentInputIndex++;

        await context.EmitEventAsync(new() { Id = Events.UserInputReceived, Data = userMessage });
    }

    [KernelFunction(Functions.GetUserConfirmation)]
    public async ValueTask GetUserConfirmationAsync(KernelProcessStepContext context)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nPlease confirm your request, or provide a new marketing request. Just say «bye» to exit: ");
        Console.ForegroundColor = ConsoleColor.White;
        var userMessage = Console.ReadLine();

        if (userMessage?.StartsWith(@"bye", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            await context.EmitEventAsync(new() { Id = Events.Exit, Data = userMessage });
            return;
        }

        state.UserInputs.Add(userMessage!);
        state.CurrentInputIndex++;

        await context.EmitEventAsync(new() { Id = Events.UserInputReceived, Data = userMessage });
    }
}

public record UserInputState
{
    public List<string> UserInputs { get; init; } = [];

    public int CurrentInputIndex { get; set; } = 0;
}

#pragma warning restore SKEXP0080
