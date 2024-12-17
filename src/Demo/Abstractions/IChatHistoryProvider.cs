using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo.Abstractions;

/// <summary>
/// Provider based access to the chat history.
/// </summary>
/// <remarks>
/// While the in-memory implementation is trivial, this abstraction demonstrates how one might
/// allow for the ability to access chat history from a remote store for a distributed service.
/// </remarks>
internal interface IChatHistoryProvider
{
    /// <summary>
    /// Provides access to the chat history.
    /// </summary>
    Task<ChatHistory> GetHistoryAsync();

    /// <summary>
    /// Commits any updates to the chat history.
    /// </summary>
    Task CommitAsync();
}
