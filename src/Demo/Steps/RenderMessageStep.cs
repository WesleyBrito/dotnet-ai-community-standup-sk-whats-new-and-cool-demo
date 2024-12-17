#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0080

using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo.Steps;

/// <summary>
/// Displays output to the user.  While in this case it is just writing to the console, in a real-world scenario this would be a more sophisticated rendering system.
/// Isolating this rendering logic from the internal logic of other process steps simplifies responsibility contract and simplifies testing and state management.
/// </summary>
/// <remarks>
/// This class is based on the Semantic Kernel example at <see href="https://github.com/microsoft/semantic-kernel/blob/dotnet-1.32.0/dotnet/samples/GettingStartedWithProcesses/Step04/Steps/RenderMessageStep.cs"/>.
/// </remarks>
internal sealed class RenderMessageStep : KernelProcessStep
{
    public static class Functions
    {
        public const string RenderDone = nameof(RenderMessageStep.RenderDone);
        public const string RenderError = nameof(RenderMessageStep.RenderError);
        public const string RenderInnerMessage = nameof(RenderMessageStep.RenderInnerMessage);
        public const string RenderMessage = nameof(RenderMessageStep.RenderMessage);
        public const string RenderUserText = nameof(RenderMessageStep.RenderUserText);
    }

    private readonly static Stopwatch stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Render an explicit message to indicate the process has completed in the expected state.
    /// </summary>
    /// <remarks>
    /// If this message isn't rendered, the process is considered to have failed.
    /// </remarks>
    [KernelFunction]
    public static void RenderDone()
    {
        Render(@"DONE!");
    }

    public static void Render(string message)
    {
        Console.WriteLine($"[{stopwatch.Elapsed:mm\\:ss}] {message}");
    }

    /// <summary>
    /// Render exception
    /// </summary>
    [KernelFunction]
    public static void RenderError(KernelProcessError error, ILogger logger)
    {
        var message = string.IsNullOrWhiteSpace(error.Message) ? @"Unexpected failure" : error.Message;
        
        Render($@"ERROR: {message} [{error.GetType().Name}]{Environment.NewLine}{error.StackTrace}");
        
        logger.LogError(@"Unexpected failure: {ErrorMessage} [{ErrorType}]", error.Message, error.Type);
    }

    /// <summary>
    /// Render user input
    /// </summary>
    [KernelFunction]
    public static void RenderUserText(string message)
    {
        Render($@"{AuthorRole.User.Label.ToUpperInvariant()}: {message}");
    }

    /// <summary>
    /// Render an assistant message from the primary chat
    /// </summary>
    [KernelFunction]
    public static void RenderMessage(ChatMessageContent message)
    {
        Render(message);
    }

    /// <summary>
    /// Render an assistant message from the inner chat
    /// </summary>
    [KernelFunction]
    public static void RenderInnerMessage(ChatMessageContent message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Render(message);
        Console.ResetColor();
    }

    public static void Render(ChatMessageContent message, bool indent = false)
    {
        var displayName = !string.IsNullOrWhiteSpace(message.AuthorName) ? $@" - {message.AuthorName}" : string.Empty;
        Render($"{(indent ? "\t" : string.Empty)}{message.Role.Label.ToUpperInvariant()}{displayName}: {message.Content}");
    }
}

#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0001

