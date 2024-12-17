using Demo.Abstractions;

using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo.Services;

internal sealed class ChatHistoryProvider : IChatHistoryProvider
{
    private readonly ChatHistory history;

    public ChatHistoryProvider(ChatHistory chatHistory)
    {
        history = chatHistory;
    }

    /// <inheritdoc/>
    public Task<ChatHistory> GetHistoryAsync() => Task.FromResult(history);

    /// <inheritdoc/>
    public Task CommitAsync()
    {
        return Task.CompletedTask;
    }
}
