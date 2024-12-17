#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0110

using Demo;
using Demo.Abstractions;
using Demo.Services;
using Demo.Steps;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

using System.Runtime.Intrinsics.X86;

const string ManagerName = "Manager";

const string ManagerInstructions =
    """
    You are a marketing manager who is responsible for coordinating the creative team.
    Capture information provided by the user for their request for a marketing campaign, specifically copywriting.
    Request confirmation without suggesting additional details.
    Once confirmed inform them you're working on the request.
    Never provide a direct answer to the user's request.
    Once completed, just return to the user the created copy only.
    """;

const string ManagerSummaryInstructions =
    """
    Summarize the most recent user request in first person command form.
    """;

const string CreativeDirectorName = "CreativeDirector";
const string CreativeDirectorInstructions =
    """
    You are a creative director who has opinions about copywriting born of a love for David Ogilvy and Harrison McCann.
    Your goal is to determine if a given copy is acceptable to print, even if it isn't perfect. 
    If not, provide insight on how to refine suggested copy without example.
    Always respond to the most recent message by evaluating and providing critique without example.    
    If copy is acceptable and meets your criteria, state that it is approved.
    If not, provide insight on how to refine suggested copy without examples.
    """;

const string CopywriterName = "Copywriter";
const string CopywriterInstructions =
    """
    You are a copywriter with ten years of experience and are known for brevity and a dry humor.
    You're laser focused on the goal at hand.
    Don't waste time with chit chat.
    Your goal is to refine and decide on one single best copy as an expert in the field.
    Just create one copy.
    Consider suggestions when refining an idea.
    """;

const string SuggestionSummaryInstructions =
    """
    Address the user directly with a summary of the response.
    """;

ChatHistory history = [];

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile(@"appsettings.json", optional: false)
                     .AddJsonFile($@"appsettings.{Environment.UserName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

builder.Services.AddOptionsWithValidateOnStart<AzureOpenAIOptions>().Bind(builder.Configuration.GetSection(nameof(AzureOpenAIOptions))).ValidateDataAnnotations();

builder.Services.AddTransient(serviceProvider =>
{
    var oaiOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;

    var kernelBuilder = Kernel.CreateBuilder()
                              .AddAzureOpenAIChatCompletion(oaiOptions.ChatModelDeploymentName, oaiOptions.Endpoint, oaiOptions.ApiKey, modelId: oaiOptions.ChatModelName)
                              .AddAzureOpenAITextToImage(oaiOptions.ImageModelDeploymentName, oaiOptions.Endpoint, oaiOptions.ApiKey, modelId: oaiOptions.ImageModelName);

    SetupAgents(kernelBuilder, kernelBuilder.Build());

    kernelBuilder.Services.AddSingleton<IChatHistoryProvider>(new ChatHistoryProvider(history));

    var kernel = kernelBuilder.Build();

    return kernel;
});

using var cancellationTokenSource = new CancellationTokenSource();
using var host = builder.Build();

var kernel = host.Services.GetRequiredService<Kernel>();

var process = SetupAgentProcess(nameof(Demo));

// Execute process
using var localProcess = await process.StartAsync(kernel, new KernelProcessEvent()
{
    Id = Events.StartProcess
});

// Demonstrate history is maintained independent of process state
////this.WriteHorizontalRule();

foreach (var message in history)
{
    RenderMessageStep.Render(message);
}

/*****************************************************************************/

static ChatCompletionAgent CreateAgent(string name, string instructions, Kernel kernel) => new()
{
    Name = name,
    Instructions = instructions,
    Kernel = kernel.Clone(),
    ////Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
    ////{
    ////    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    ////    Temperature = 0,
    ////}),
};

static void SetupAgents(IKernelBuilder builder, Kernel kernel)
{
    // Create and inject primary agent into service collection
    var managerAgent = CreateAgent(ManagerName, ManagerInstructions, kernel.Clone());
    builder.Services.AddKeyedSingleton(ManagerAgentStep.AgentServiceKey, managerAgent);

    // Create and inject group chat into service collection
    SetupGroupChat(builder, kernel);

    // Create and inject reducers into service collection
    builder.Services.AddKeyedSingleton(ManagerAgentStep.ReducerServiceKey, SetupReducer(kernel, ManagerSummaryInstructions));
    builder.Services.AddKeyedSingleton(AgentGroupChatStep.ReducerServiceKey, SetupReducer(kernel, SuggestionSummaryInstructions));
}

static ChatHistorySummarizationReducer SetupReducer(Kernel kernel, string instructions) => new(kernel.GetRequiredService<IChatCompletionService>(), 1)
{
    SummarizationInstructions = instructions
};

static void SetupGroupChat(IKernelBuilder builder, Kernel kernel)
{
    var agentCreativeDirector = CreateAgent(CreativeDirectorName, CreativeDirectorInstructions, kernel.Clone());
    var agentCopywriter = CreateAgent(CopywriterName, CopywriterInstructions, kernel.Clone());

    var selectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
               Determine which participant takes the next turn in a conversation based on the most recent participant.
               State only the name of the participant to take the next turn.
               No participant should take more than one turn in a row.
                
               Choose only from these participants:
                - {{{CreativeDirectorName}}}
                - {{{CopywriterName}}}
                
               Always follow these rules when selecting the next participant:
                - After {{{CopywriterName}}}, it is {{{CreativeDirectorName}}}'s turn.
                - After {{{CreativeDirectorName}}}, it is {{{CopywriterName}}}'s turn.
                                
               History:
               {{$history}}
               """,
            safeParameterNames: "history");

    var terminationFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
               Determine if the copy has been approved. If so, respond with a single word: yes

               History:
               {{$history}}
               """,
            safeParameterNames: @"history");

    ChatHistoryTruncationReducer strategyReducer = new(1);

    AgentGroupChat chat = new(agentCreativeDirector, agentCopywriter)
    {
        LoggerFactory = NullLoggerFactory.Instance,
        ExecutionSettings = new()
        {
            SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel.Clone())
            {
                HistoryReducer = strategyReducer,
                HistoryVariableName = @"history",
                InitialAgent = agentCopywriter, // Always start with the copywriter agent...
                ResultParser = (result) => result.GetValue<string>() ?? CopywriterName,
            },

            TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, kernel.Clone())
            {
                Agents = [agentCreativeDirector], // Only the creative director may approve...
                HistoryVariableName = @"history",
                MaximumIterations = 10,
                HistoryReducer = strategyReducer,
                ResultParser = (result) => result.GetValue<string>()?.Contains(@"yes", StringComparison.OrdinalIgnoreCase) ?? false,
            }
        }
    };

    builder.Services.AddSingleton(chat);
}

static KernelProcess SetupAgentProcess(string processName)
{
    ProcessBuilder process = new(processName);

    var welcomeStep = process.AddStepFromType<WelcomeStep>();
    var userInputStep = process.AddStepFromType<UserInputStep>();
    var renderMessageStep = process.AddStepFromType<RenderMessageStep>();
    var managerAgentStep = process.AddStepFromType<ManagerAgentStep>();
    var agentGroupStep = process.AddStepFromType<AgentGroupChatStep>();
    var imageCreatorStep = process.AddStepFromType<ImageCreatorStep>();

    AttachErrorStep(
        userInputStep,
        UserInputStep.Functions.GetUserInput);

    AttachErrorStep(
        managerAgentStep,
        ManagerAgentStep.Functions.InvokeAgent,
        ManagerAgentStep.Functions.InvokeGroup,
        ManagerAgentStep.Functions.ReceiveResponse);

    AttachErrorStep(
        agentGroupStep,
        AgentGroupChatStep.Functions.InvokeAgentGroup);

    // Entry point
    process.OnInputEvent(Events.StartProcess)
        .SendEventTo(new ProcessFunctionTargetBuilder(welcomeStep));

    // Once the welcome message has been shown, request user input
    welcomeStep.OnFunctionResult(WelcomeStep.Functions.WelcomeMessage)
             .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep, UserInputStep.Functions.GetUserInput));

    // Pass user input to primary agent
    userInputStep
        .OnEvent(Events.UserInputReceived)
        .SendEventTo(new ProcessFunctionTargetBuilder(managerAgentStep, ManagerAgentStep.Functions.InvokeAgent))
        ////.SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderUserText, parameterName: "message"))
        ;

    ////// Process completed
    ////userInputStep
    ////    .OnEvent(Events.UserInputComplete)
    ////    .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderDone))
    ////    .StopProcess();

    // Render response from primary agent
    managerAgentStep
        .OnEvent(Events.Agents.AgentResponse)
        .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderMessage, parameterName: "message"));

    // Request is complete
    managerAgentStep
        .OnEvent(Events.UserInputComplete)
        .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderDone))
        .StopProcess();

    managerAgentStep
        .OnEvent(Events.Agents.AgentRequestUserConfirmation)
        .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep, UserInputStep.Functions.GetUserConfirmation));

    // Request more user input
    managerAgentStep
        .OnEvent(Events.Agents.AgentResponded)
        .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep, UserInputStep.Functions.GetUserInput));

    // Delegate to inner agents
    managerAgentStep
        .OnEvent(Events.Agents.AgentWorking)
        .SendEventTo(new ProcessFunctionTargetBuilder(managerAgentStep, ManagerAgentStep.Functions.InvokeGroup));

    // Provide input to inner agents
    managerAgentStep
        .OnEvent(Events.Agents.GroupInput)
        .SendEventTo(new ProcessFunctionTargetBuilder(agentGroupStep, parameterName: "input"));

    // Render response from inner chat (for visibility)
    agentGroupStep
        .OnEvent(Events.Agents.GroupMessage)
        .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderInnerMessage, parameterName: "message"));

    // Provide inner response to primary agent
    agentGroupStep
        .OnEvent(Events.Agents.GroupCompleted)
        .SendEventTo(new ProcessFunctionTargetBuilder(imageCreatorStep, ImageCreatorStep.Functions.CreateImageForCopy, parameterName: "copy"));

    imageCreatorStep
        .OnEvent(Events.ImageCreated)
        .SendEventTo(new ProcessFunctionTargetBuilder(managerAgentStep, ManagerAgentStep.Functions.ReceiveResponse, parameterName: "response"));


    var kernelProcess = process.Build();

    return kernelProcess;

    void AttachErrorStep(ProcessStepBuilder step, params string[] functionNames)
    {
        foreach (var functionName in functionNames)
        {
            step.OnFunctionError(functionName)
                .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderError, "error"))
                .StopProcess();
        }
    }
}

#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0110
