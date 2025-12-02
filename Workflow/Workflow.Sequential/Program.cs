using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
Configuration configuration = ConfigurationManager.GetConfiguration();


AzureOpenAIClient client = new(
    new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey));

ChatClient chatClient = client.GetChatClient(configuration.ChatDeploymentName4);

ChatClientAgent summaryAgent = chatClient.CreateAIAgent(name: "SummaryAgent", instructions: "Summarize the text given to you in less than 25 words.");

ChatClientAgent translateAgent = chatClient.CreateAIAgent(name: "TranslateAgent", instructions: "Translate the text given to you to Vietnamese.");

Workflow workflow = AgentWorkflowBuilder.BuildSequential(summaryAgent, translateAgent);

string shortStory = """
                        The Happy Prince and Other Tales (or Stories) is a collection of bedtime stories for children by Oscar Wilde, first published in May 1888. It contains five stories that are highly popular among children and frequently read in schools: The Happy Prince, The Nightingale and the Rose, The Selfish Giant. The Devoted Friend, and The Remarkable Rocket. The short stories are valued for their morals, and have been made into animated films. In 2003, the second through fourth stories were adapted by Lupus Films and Terraglyph Interactive Studios into the three-part series Wilde Stories for Channel 4. The stories are regarded as classics of children's literature.
                    """;
var message = new List<ChatMessage> { new(ChatRole.User, shortStory) };

StreamingRun run = await InProcessExecution.StreamAsync(workflow, message);
await run.TrySendMessageAsync(new TurnToken(true));

List<ChatMessage> responses = new();
await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
{
    if (evt is WorkflowOutputEvent complete)
    {
        responses = (List<ChatMessage>)complete.Data;
        break;
    }
}

foreach (ChatMessage msg in responses.Where(x => x.Role == ChatRole.User))
{
    Utils.WriteLineGreen(msg.AuthorName ?? "UnKnow");
    Console.WriteLine($"{msg.Text}");
    Utils.Separator();
}