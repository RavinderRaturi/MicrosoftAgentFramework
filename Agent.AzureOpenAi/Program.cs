using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using ProjectConstant;
using System.ClientModel;


// Load configuration so the app can read secrets stored via User Secrets.
// This avoids hardcoding keys and keeps them out of source control.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()   // Pulls values from the secrets.json linked to this project
    .Build();

// Fetch the OpenAI key from the secret store.
// If this returns null or empty, the key was never added.
var openAiKey = config[ProjectConstant.UserSecretsKey.ApiKeys_AzureOpenAI];

// Print whether the key is available so you immediately know if setup is broken.
Console.WriteLine(ProjectConstant.UserSecretsKey.ApiKeys_AzureOpenAI +
    $" status :  {(string.IsNullOrWhiteSpace(openAiKey) ? "missing" : "loaded")}");

// Create the Azure OpenAI client using your endpoint and the key.
// Without these two inputs, the client cannot authenticate.
Azure.AI.OpenAI.AzureOpenAIClient client = new AzureOpenAIClient(
    new Uri(UserSecretsKey.ApiKeys_AzureOpenAIEndPoint),
    new ApiKeyCredential(openAiKey));

// Build an AI agent on top of the chat client.
// This wraps your model and gives you a simpler high level interface to run tasks.
AIAgent agent = client
    .GetChatClient(UserSecretsKey.ApiKeys_AzureOpenAIModel.ToString())
    .CreateAIAgent();

// Run a simple prompt and wait for a complete response.
// Useful for synchronous, one shot queries.
AgentRunResponse response = await agent.RunAsync("What comes after 12 in a counting sequence?");

// Print a separator to make console output readable.
Console.WriteLine("---");
Console.WriteLine(response);

Console.WriteLine("---");

// Streamed response example.
// This lets you process the answer chunk by chunk instead of waiting for the end.
// Good for long answers or building real time interfaces.
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);   // Write incremental chunks as they arrive
}




