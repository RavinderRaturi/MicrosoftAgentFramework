
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using Shared.Extensions;
using System.ClientModel;


Configuration configuration = ConfigurationManager.GetConfiguration();

AzureOpenAIClient azureOpenAIClient =
    new AzureOpenAIClient(
        new Uri(configuration.AzureOpenAiEndpoint),
        new System.ClientModel.ApiKeyCredential(configuration.AzureOpenAiKey));
OpenAIClient openAIClient =
    new OpenAIClient(new ApiKeyCredential(configuration.OpenAiApiKey));

ChatClientAgent azureOpenAiAgent = azureOpenAIClient.GetChatClient(configuration.ChatDeploymentName).CreateAIAgent();

ChatClientAgent openAiAgent = openAIClient.GetChatClient(configuration.ChatDeploymentName).CreateAIAgent();


Scenario scenario = Scenario.Images;

AgentRunResponse response;
switch (scenario)
{
    case Scenario.Images:
        {
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                new TextContent("What is in this Image?"),
             new UriContent("https://images.unsplash.com/photo-1506744038136-46273834b3fb", "image/jpg")
                 ]
                 ));
            ShowResponse(response);

            string path = Path.Combine("SampleData", "image.jpg");

            string base64Pdf = Convert.ToBase64String(File.ReadAllBytes(path));
            string dataUri = $"data:image/jpg;base64,{base64Pdf}";
            response = await openAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                new TextContent("What is in this Image?"),
                new DataContent(dataUri, "image/jpg")
                ]));


        }

        break;
    default:
        throw new ArgumentOutOfRangeException();
}


void ShowResponse(AgentRunResponse agentRunResponse)
{
    Console.WriteLine(agentRunResponse);
    agentRunResponse.Usage.OutputAsInformation();
}

enum Scenario
{ Text, Pdf, Images, }