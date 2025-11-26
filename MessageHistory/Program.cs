using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.Text.Json;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


Configuration configuration = ConfigurationManager.GetConfiguration();

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(configuration.AzureOpenAiEndpoint), new System.ClientModel.ApiKeyCredential(configuration.AzureOpenAiKey));

ChatClientAgent agent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(instructions: "You are a helpful assistant that has a habbit of forgetting message history.");

//var agent = client
//    .GetChatClient(configuration.ChatDeploymentName)
//    .CreateAIAgent(instructions: "You are a helpful assistant that has a habbit of forgetting message history.");


AgentThread thread;

const bool activateMessageHistory = true;

if (activateMessageHistory)
{
    thread = await AgentThreadPersistaence.LoadHistoricalMessages(agent)!;
}
else
{
    thread = agent.GetNewThread();
}


while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if(input=="exit" || input == "quit")
    {
        break;
    }
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessage message = new ChatMessage(ChatRole.User, input);
        await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(message, thread))
        {
            Console.Write(update);
        }
    }
    Utils.Separator();

    if (activateMessageHistory)
    {
        await AgentThreadPersistaence.SaveHistoricalMessages(thread);
    } 
}


public static class AgentThreadPersistaence
{

    private static string storagePath => Path.Combine(Path.GetTempPath(), "historicalMessages.json");

    public static async Task<AgentThread?> LoadHistoricalMessages(ChatClientAgent agent)
    {
        if (!File.Exists(storagePath))
        {
            return agent.GetNewThread();
        }

        JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync
            (storagePath));
        AgentThread historyThread = agent.DeserializeThread(jsonElement);
        await RestoreConsole(historyThread);
        return historyThread;

    }


    private static async Task RestoreConsole(AgentThread messageHistory)
    {
        ChatClientAgentThread chatClientAgentThread = (ChatClientAgentThread)messageHistory;
        if (chatClientAgentThread.MessageStore != null)
        {
            IEnumerable<ChatMessage> messages = await chatClientAgentThread.MessageStore.GetMessagesAsync();
            foreach (ChatMessage message in messages)
            {
                if (message.Role == ChatRole.User)
                {
                    Console.WriteLine($"User: {message.Text}");
                }
                else if (message.Role == ChatRole.Assistant)
                {
                    Console.WriteLine($"Assistant: {message.Text}");
                    Console.WriteLine();
                    Console.WriteLine(string.Empty.PadLeft(50, '-'));
                    Console.WriteLine();
                }
            }
        }
    }

    public static async Task SaveHistoricalMessages(AgentThread thread)
    {
        var messages = thread.Serialize();
        await File.WriteAllTextAsync(storagePath, System.Text.Json.JsonSerializer.Serialize(messages));
    }

}
