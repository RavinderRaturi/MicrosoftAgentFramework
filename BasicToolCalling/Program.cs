using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;


Configuration configuration = ConfigurationManager.GetConfiguration();

AzureOpenAIClient client =
    new AzureOpenAIClient(
        new Uri(configuration.AzureOpenAiEndpoint),
        new System.ClientModel.ApiKeyCredential(configuration.AzureOpenAiKey)
        );

ChatClientAgent agent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
    instructions: "You are a helpful assistant and an amazing time expert "
    , tools:
    [
        AIFunctionFactory.Create(Tools.GetCurrentDateTime,"get_current_date_time"),
        AIFunctionFactory.Create(Tools.GetCurrentTimeZone,"get_current_time_zone")
    ]
    );

AgentThread thread = agent.GetNewThread();

while (true)
{
    Console.Write("> ");
    string userInput = Console.ReadLine() ?? string.Empty;
    ChatMessage message = new ChatMessage(ChatRole.User, userInput);
    AgentRunResponse response = await agent.RunAsync(message, thread);
    Console.WriteLine(response);
    Utils.Separator();
}



public static class Tools
{
    public static DateTime GetCurrentDateTime(TimeType type)
    {
        return type switch
        {
            TimeType.UTC => DateTime.UtcNow,
            TimeType.Local => DateTime.Now,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Time zoon not recognised.")
        };
    }

    public static string GetCurrentTimeZone()
    {
        return TimeZoneInfo.Local.DisplayName;
    }


    public enum TimeType
    {
        UTC,
        Local
    }
}