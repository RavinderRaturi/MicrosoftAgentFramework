using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using Shared;
using System.ClientModel;
using System.Text;

Configuration configuration = ConfigurationManager.GetConfiguration();

AzureOpenAIClient client = new
    (
    new(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey)
    );

McpClient gitHubMcpClient = await McpClient.CreateAsync
    (
       new HttpClientTransport
        (
            new HttpClientTransportOptions
            {
                TransportMode = HttpTransportMode.StreamableHttp,
                Endpoint = new("https://api.githubcopilot.com/mcp/"),
                AdditionalHeaders = new Dictionary<string, string>
                    {
                        {"Authorization", configuration.GitHubPatToken }
                    }

            }
        )
    );


IList<McpClientTool> toolsInGitHubMcp = await gitHubMcpClient.ListToolsAsync();


AIAgent agent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
    instructions: "You are a GitHub Expert",
    tools: toolsInGitHubMcp.Cast<AITool>().ToList()
    )
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AgentThread thread = agent.GetNewThread();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    ChatMessage message = new ChatMessage(ChatRole.User, input);
    AgentRunResponse response = await agent.RunAsync(message, thread);
    Console.WriteLine(response);
    Utils.Separator();
}




async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append(
            $" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key}={x.Value}]"))}"
            );
    }
    Utils.WriteLineDarkGray(functionCallDetails.ToString());

    return await next(context, cancellationToken);
}