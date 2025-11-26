using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Shared;
using Shared.Extensions;

// System message
Utils.WriteLineYellow("Initializing Ollama client. Targeting local runtime at port 11434.");

IChatClient client = new OllamaApiClient("http://localhost:11434", "llama3.2:1b");

Utils.WriteLineYellow("Creating chat agent for model 'llama3.2:1b'. Preparing execution pipeline.");

ChatClientAgent agent = new(client);

Utils.WriteLineYellow("Executing first request. Non streaming mode. Full output expected in one block.");

AgentRunResponse response = await agent.RunAsync("What comes after 5?");

Utils.WriteLineGreen("Response received:");
Utils.WriteLineGreen(response.ToString());

Utils.WriteLineDarkGray($"- Input Tokens: {response.Usage?.InputTokenCount}");
Utils.WriteLineDarkGray($"- Output Tokens: {response.Usage?.OutputTokenCount} " +
                        $"({response.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");


// Streaming banner
Utils.WriteLineYellow(@"
 ~~~  ~~~   ~~~~~  ~~~~~~~   ~~~~
   ~  STREAMING  ~
 ~~~~   ~~~~~  ~~~   ~~~~~    ~~~
");

Utils.WriteLineYellow("Streaming mode active. Tokens will arrive incrementally. Hold for partial output.");

List<AgentRunResponseUpdate> updates = [];
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Tell me a story of a sea monster."))
{
    updates.Add(update);

    // AI partial output
    Console.Write(update.ToString(), ConsoleColor.Red);//.WriteLine(update.ToString());
}

AgentRunResponse collectedResponseFromStreaming = updates.ToAgentRunResponse();

Utils.WriteLineDarkGray($"- Input Tokens (Streaming): {collectedResponseFromStreaming.Usage?.InputTokenCount}");
Utils.WriteLineDarkGray($"- Output Tokens (Streaming): {collectedResponseFromStreaming.Usage?.OutputTokenCount} " +
                        $"({collectedResponseFromStreaming.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");

Utils.Separator();

Utils.WriteLineYellow("\nStreaming complete. Model idle. Session closed.");
