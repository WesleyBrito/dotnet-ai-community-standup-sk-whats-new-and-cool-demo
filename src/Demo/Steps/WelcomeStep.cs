#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;

namespace Demo.Steps;

internal sealed class WelcomeStep : KernelProcessStep
{
    public static class Functions
    {
        public const string WelcomeMessage = nameof(WelcomeStep.WelcomeMessage);
    }

    [KernelFunction(Functions.WelcomeMessage)]
    public static void WelcomeMessage()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Welcome to this Demo!\n");
        Console.ResetColor();
    }
}

#pragma warning restore SKEXP0080
