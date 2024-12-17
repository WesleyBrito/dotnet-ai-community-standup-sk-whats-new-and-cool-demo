#pragma warning disable SKEXP0110

using Demo.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.History;

namespace Demo.Extensions;

/// <summary>
/// Convenience extensions for agent based process patterns.
/// </summary>
internal static class KernelExtensions
{
    /// <summary>
    /// Gets the chat history from the (singleton) instance of <see cref="IChatHistoryProvider"/>.
    /// </summary>
    public static IChatHistoryProvider GetHistory(this Kernel kernel) => kernel.Services.GetRequiredService<IChatHistoryProvider>();

    /// <summary>
    /// Access an agent as a keyed service.
    /// </summary>
    public static TAgent GetAgent<TAgent>(this Kernel kernel, string key) where TAgent : KernelAgent => kernel.Services.GetRequiredKeyedService<TAgent>(key);

    /// <summary>
    /// Summarize chat history using reducer accessed as a keyed service.
    /// </summary>
    /// <exception cref="InvalidDataException">Thrown when no summary is available.</exception>
    public static async Task<string> SummarizeHistoryAsync(this Kernel kernel, string key, IReadOnlyList<ChatMessageContent> history)
    {
        var reducer = kernel.Services.GetRequiredKeyedService<ChatHistorySummarizationReducer>(key);

        var reducedResponse = await reducer.ReduceAsync(history);

        var summary = reducedResponse?.First() ?? throw new InvalidDataException(@"No summary available");

        return summary.ToString();
    }
}

#pragma warning restore SKEXP0110
