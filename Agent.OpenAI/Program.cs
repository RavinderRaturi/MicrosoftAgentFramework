using Microsoft.Extensions.Configuration;
using Microsoft.Agents.AI;
using ProjectConstant;
using OpenAI;

// Build configuration to load secrets from the project's User Secrets store.
// This keeps API keys out of your source code.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Pull the OpenAI API key from secrets.
// If this comes back empty, the rest of the code cannot function.
var openAiKey = config[ProjectConstant.UserSecretsKey.ApiKeys_OpenAI];

// Quick diagnostic output so you know immediately if the key is missing.
Console.WriteLine(ProjectConstant.UserSecretsKey.ApiKeys_OpenAI +
    $" status : {(string.IsNullOrWhiteSpace(openAiKey) ? "missing" : "loaded")}");

// Create the OpenAI client using the loaded key.
// This is the entry point for all subsequent chat and agent calls.
var client = new OpenAIClient(openAiKey);

// Build an AI agent using the configured model.
// This wraps the lower level chat API into a higher level agent interface.
AIAgent agent = client
    .GetChatClient(ProjectConstant.UserSecretsKey.ApiKeys_OpenAIModel)
    .CreateAIAgent();

// Run a single prompt and wait for the complete response.
// Good for basic one shot tasks.
AgentRunResponse agentRunResponse = await agent.RunAsync("What is the capital of Vietnam?");

// Print response cleanly.
Console.WriteLine("---");
Console.WriteLine(agentRunResponse);

// Stream a second answer chunk by chunk.
// Useful when you want incremental output without waiting for completion.
Console.WriteLine("---");
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Give me a recipe for pancakes in less than 3 lines"))
{
    Console.Write(update);
}
