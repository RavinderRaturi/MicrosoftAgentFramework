using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using ProjectConstant;
using System.Net;
using System.Reflection;

// Load secrets from User Secrets.
// Avoids hardcoding keys and keeps secrets out of source control.
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Read the Azure OpenAI key from secrets.
// If empty, authentication dependent code will fail.
var key = config[ProjectConstant.UserSecretsKey.ApiKeys_AzureOpenAI];

// Print key load status so failures are easy to diagnose.
Console.WriteLine(ProjectConstant.UserSecretsKey.ApiKeys_AzureOpenAI +
    $" status : {(string.IsNullOrWhiteSpace(key) ? "missing" : "loaded")}");

// Create a PersistentAgentsClient using Azure CLI credentials.
// This requires Azure CLI to be installed and logged in.
PersistentAgentsClient client = new(UserSecretsKey.ApiKeys_AzureFoundryEndPoint, new AzureCliCredential());

Response<PersistentAgent>? aiFoundryAgent = null;

try
{
    // Create a new persistent agent on Azure Foundry.
    // This allocates a server side agent instance.
    aiFoundryAgent = await client.Administration.CreateAgentAsync(
        UserSecretsKey.ApiKeys_AzureOpenAIModel,
        "MyFirstAgent",
        "Some description",
        "You are a nice AI");

    // Bind a ChatClientAgent to the created agent id.
    ChatClientAgent agent = await client.GetAIAgentAsync(aiFoundryAgent.Value.Id);

    // Start a fresh thread for this conversation.
    AgentThread thread = agent.GetNewThread();

    // Run a simple synchronous query.
    AgentRunResponse response = await agent.RunAsync("What is the capital of France?", thread);
    Console.WriteLine(response);

    Console.WriteLine("---");

    //// Run a streaming query and print partial updates as they arrive.
    //await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("How to make soup?", thread))
    //{
    //    Console.Write(update);
    //}
}
finally
{
    // Ensure the created agent is cleaned up even if something fails.
    if (aiFoundryAgent != null)
    {
        await client.Administration.DeleteAgentAsync(aiFoundryAgent.Value.Id);
    }
}
