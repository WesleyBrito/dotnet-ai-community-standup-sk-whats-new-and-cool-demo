#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0110

using Demo.Extensions;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

using System.ComponentModel;
using System.Text.Json;

namespace Demo.Steps;

/// <summary>
/// A step that defines actions for the primary agent which is responsible for interacting with
/// the user as well as as delegating to a group of agents.
/// </summary>
internal sealed class ManagerAgentStep : KernelProcessStep
{
    public static class Functions
    {
        public const string InvokeAgent = nameof(InvokeAgent);
        public const string InvokeGroup = nameof(InvokeGroup);
        public const string ReceiveResponse = nameof(ReceiveResponse);
    }

    public const string AgentServiceKey = $@"{nameof(ManagerAgentStep)}:{nameof(AgentServiceKey)}";

    public const string ReducerServiceKey = $"{nameof(ManagerAgentStep)}:{nameof(ReducerServiceKey)}";

    [KernelFunction(Functions.InvokeAgent)]
    public static async Task InvokeAgentAsync(KernelProcessStepContext context, Kernel kernel, string userInput, ILogger logger)
    {
        // Get the chat history
        var historyProvider = kernel.GetHistory();
        var history = await historyProvider.GetHistoryAsync();

        // Add the user input to the chat history
        history.Add(new ChatMessageContent(AuthorRole.User, userInput));

        // Obtain the agent response
        var agent = kernel.GetAgent<ChatCompletionAgent>(AgentServiceKey);

        await foreach (var message in agent.InvokeAsync(history))
        {
            // Capture each response
            history.Add(message);

            // Emit event for each agent response
            await context.EmitEventAsync(new() { Id = Events.Agents.AgentResponse, Data = message });
        }

        // Commit any changes to the chat history
        await historyProvider.CommitAsync();

        // Evaluate current intent
        var intent = await IsRequestingUserInputAsync(kernel, history, logger);

        var intentEventId = Events.UserInputComplete;

        if (intent.IsRequestingConfirmation)
        {
            intentEventId = Events.Agents.AgentRequestUserConfirmation;
        }
        else if (intent.IsRequestingUserInput)
        {
            intentEventId = Events.Agents.AgentResponded;
        }
        else if (intent.IsWorking)
        {
            intentEventId = Events.Agents.AgentWorking;
        }

        await context.EmitEventAsync(new() { Id = intentEventId });
    }

    [KernelFunction(Functions.InvokeGroup)]
    public static async Task InvokeGroupAsync(KernelProcessStepContext context, Kernel kernel)
    {
        // Get the chat history
        var historyProvider = kernel.GetHistory();
        var history = await historyProvider.GetHistoryAsync();

        // Summarize the conversation with the user to use as input to the agent group
        var summary = await kernel.SummarizeHistoryAsync(ReducerServiceKey, history);

        await context.EmitEventAsync(new() { Id = Events.Agents.GroupInput, Data = summary });
    }

    [KernelFunction(Functions.ReceiveResponse)]
    public static async Task ReceiveResponseAsync(KernelProcessStepContext context, Kernel kernel, string response)
    {
        // Get the chat history
        var historyProvider = kernel.GetHistory();
        var history = await historyProvider.GetHistoryAsync();

        // Proxy the inner response
        var agent = kernel.GetAgent<ChatCompletionAgent>(AgentServiceKey);
        ChatMessageContent message = new(AuthorRole.Assistant, response) { AuthorName = agent.Name };
        history.Add(message);

        await context.EmitEventAsync(new() { Id = Events.Agents.AgentResponse, Data = message });

        await context.EmitEventAsync(new() { Id = Events.Agents.AgentResponded });
    }

    private static readonly OpenAI.Chat.ChatResponseFormat IntentResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(jsonSchemaFormatName: "intent_result", jsonSchema: BinaryData.FromString(JsonSchemaGenerator.FromType<IntentResult>()), jsonSchemaIsStrict: true);

    private static async Task<IntentResult> IsRequestingUserInputAsync(Kernel kernel, ChatHistory history, ILogger logger)
    {
        ChatHistory localHistory = [new ChatMessageContent(AuthorRole.System, @"Analyze the conversation."), .. history.TakeLast(1)];

        var service = kernel.GetRequiredService<IChatCompletionService>();

        var response = await service.GetChatMessageContentAsync(localHistory, new AzureOpenAIPromptExecutionSettings { ResponseFormat = IntentResponseFormat });
        var intent = JsonSerializer.Deserialize<IntentResult>(response.ToString())!;

        logger.LogTrace(@"{StepName} Response Intent - {IsRequestingUserInput}: {Rationale}", nameof(ManagerAgentStep), intent.IsRequestingUserInput, intent.Rationale);

        return intent;
    }

    [DisplayName("IntentResult")]
    [Description("this is the result description")]
    public sealed class IntentResult
    {
        [Description(@"True if you need the user to tell you what to do. False if you are asking a question or requesting confirmation.")]
        public bool IsRequestingUserInput { get; set; }

        [Description(@"True if you are asking a question or requesting confirmation.")]
        public bool IsRequestingConfirmation { get; set; }

        [Description(@"True if the user request is clear to work on. False if you are asking a question.")]
        public bool IsWorking { get; set; }

        [Description(@"Rationale for the value assigned to IsRequestingUserInput and IsRequestingConfirmation")]
        public string Rationale { get; set; }

        public IntentResult(bool isRequestingUserInput, bool isWorking, string rationale)
        {
            IsRequestingUserInput = isRequestingUserInput;
            IsWorking = isWorking;
            Rationale = rationale;
        }
    }
}

#pragma warning restore SKEXP0110
#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0001
