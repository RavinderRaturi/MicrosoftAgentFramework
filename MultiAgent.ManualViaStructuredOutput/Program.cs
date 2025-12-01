using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.ComponentModel;

Configuration configuration = ConfigurationManager.GetConfiguration();

AzureOpenAIClient client = new(new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey));


ChatClient chatClientMini = client.GetChatClient(configuration.ChatDeploymentName);
ChatClient chatClient = client.GetChatClient("gpt-4.1");

Console.WriteLine("Multi-Agent Chat with Manual via Structured Output");

string userPrompt = Console.ReadLine();

//Determine intial intent
ChatClientAgent intentAgent = chatClientMini.CreateAIAgent(name: "IntentAgent", instructions: "Determine what type of question was asked. Never answer yourself.");

AgentRunResponse<IntentResult> initialResponse = await intentAgent.RunAsync<IntentResult>(userPrompt);
IntentResult intentResult = initialResponse.Result;


switch (intentResult.Intent)
{
    case Intent.MusicQuestion:
        {
            Console.WriteLine("Routing to Music Agent");
            break;
        }

    case Intent.MovieQuestion:
        {
            Console.WriteLine("Routing to Movie Agent");
            break;
        }
    case Intent.Unknown:
        {
            Console.WriteLine("Unable to determine intent. Routing to General Agent");
            break;
        }
    default:
        {
            throw new ArgumentOutOfRangeException();
        }
}



public class IntentResult
{
    [Description("What type of question is this?")]
    public Intent Intent { get; set; }
}



public enum Intent { MusicQuestion, MovieQuestion, Unknown }