namespace Demo;

internal static class Events
{
    public static readonly string StartProcess = nameof(StartProcess);

    public static readonly string UserInputReceived = nameof(UserInputReceived);
    
    public static readonly string UserInputComplete = nameof(UserInputComplete);
    
    public static readonly string ImageCreated = nameof(ImageCreated);

    public static readonly string Exit = nameof(Exit);

    internal static class Agents
    {
        public static readonly string AgentRequestUserConfirmation = nameof(AgentRequestUserConfirmation);

        public static readonly string AgentResponse = nameof(AgentResponse);

        public static readonly string AgentResponded = nameof(AgentResponded);

        public static readonly string AgentWorking = nameof(AgentWorking);

        public static readonly string GroupInput = nameof(GroupInput);
        
        public static readonly string GroupMessage = nameof(GroupMessage);
        
        public static readonly string GroupCompleted = nameof(GroupCompleted);
    }
}
