#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;

namespace Demo.Steps;

internal sealed class WelcomeStep : KernelProcessStep
{
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members

    public static class Functions
    {
        public const string WelcomeMessage = nameof(WelcomeStep.WelcomeMessage);
    }

#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members

    [KernelFunction(Functions.WelcomeMessage)]
    public static void WelcomeMessage()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Welcome to this Demo!\n");
        Console.ResetColor();
    }
}

#pragma warning restore SKEXP0080
