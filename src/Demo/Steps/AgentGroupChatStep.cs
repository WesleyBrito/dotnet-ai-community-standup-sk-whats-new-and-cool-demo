#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0110

using Demo.Extensions;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo.Steps;

internal sealed class AgentGroupChatStep : KernelProcessStep
{
    public const string ReducerServiceKey = $@"{nameof(AgentGroupChatStep)}:{nameof(ReducerServiceKey)}";

    public static class Functions
    {
        public const string InvokeAgentGroup = nameof(InvokeAgentGroup);
    }

    [KernelFunction(Functions.InvokeAgentGroup)]
    public static async Task InvokeAgentGroupAsync(KernelProcessStepContext context, Kernel kernel, string input)
    {
        var chat = kernel.GetRequiredService<AgentGroupChat>();

        chat.IsComplete = false;

        ChatMessageContent message = new(AuthorRole.User, input);
        chat.AddChatMessage(message);
        await context.EmitEventAsync(new() { Id = Events.Agents.GroupMessage, Data = message });

        await foreach (var response in chat.InvokeAsync())
        {
            await context.EmitEventAsync(new() { Id = Events.Agents.GroupMessage, Data = response });
        }

        var history = await chat.GetChatMessagesAsync().Reverse().ToArrayAsync();

        // Summarize the group chat as a response to the primary agent
        var summary = await kernel.SummarizeHistoryAsync(ReducerServiceKey, history);

        await context.EmitEventAsync(new() { Id = Events.Agents.GroupCompleted, Data = summary });
    }
}

#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0110
