using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

// Announce that the client setup is starting.
// Users should know that this connects to a local Ollama instance.
Console.WriteLine("Initializing Ollama client. Targeting local runtime at port 11434.");

// Configure the Ollama chat client.
// The second argument selects the model. Make sure the model exists via `ollama list`.
IChatClient client = new OllamaApiClient("http://localhost:11434", "llama3.2:1b");

Console.WriteLine("Creating chat agent for model 'llama3.2:1b'. Preparing execution pipeline.");
ChatClientAgent agent = new(client);

Console.WriteLine("Executing first request. Non streaming mode. Full output expected in one block.");
AgentRunResponse response = await agent.RunAsync("What comes after 5?");
Console.WriteLine("Response received:");
Console.WriteLine(response);

// Switch to streaming mode banner
Console.WriteLine(@"
 ~~~  ~~~   ~~~~~  ~~~~~~~   ~~~~
   ~  STREAMING  ~
 ~~~~   ~~~~~  ~~~   ~~~~~    ~~~
");

Console.WriteLine("Streaming mode active. Tokens will arrive incrementally. Hold for partial output.");

// Streaming execution with incremental token updates.
// Useful for long responses or real time UI updates.
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Tell me a story of a sea monster."))
{
    Console.Write(update);
}

Console.WriteLine("\nStreaming complete. Model idle. Session closed.");
