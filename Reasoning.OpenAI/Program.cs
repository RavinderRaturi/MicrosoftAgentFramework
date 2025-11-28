using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MicrosoftAgentFramework.Utilities.Extensions;
using OpenAI;
using OpenAI.Chat;
using Shared;
using Shared.Extensions;
using System.ClientModel;

// Load configuration values like endpoint and API key.
// This centralizes secrets and prevents hardcoding them.
Configuration configuration = ConfigurationManager.GetConfiguration();

// Create an Azure OpenAI client configured with your endpoint and API key.
// ApiKeyCredential wraps the secret so the SDK can authenticate.
// NetworkTimeout increases the timeout for long running reasoning models.
// A longer timeout prevents premature cancellation during extended reasoning.
AzureOpenAIClient azureOpenAiClient = new(
    new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey),
    new AzureOpenAIClientOptions
    {
        NetworkTimeout = TimeSpan.FromMinutes(5)
    }
);

// Create an agent using the default reasoning settings of gpt-5-mini.
// CreateAIAgent attaches the Agent Framework wrapper around the raw chat client.
// The wrapper handles tool calling, state, and reasoning orchestration.
ChatClientAgent agentDefault = azureOpenAiClient
    .GetChatClient("gpt-5-mini")
    .CreateAIAgent();

// Call the agent with a simple question.
// RunAsync executes the reasoning pipeline and returns a structured response.
// AgentRunResponse contains model output, tool calls, and usage information.
AgentRunResponse response1 = await agentDefault.RunAsync(
    "What is the Capital of France and how many people live there?"
);

// Print the entire response object.
// The override prints both the answer and metadata.
Console.WriteLine(response1);

// Output usage info using an extension method that formats token consumption.
response1.Usage.OutputAsInformation();

Utils.Separator();

// Create a second agent but override the reasoning effort.
// ReasoningEffortLevel controls how much internal reasoning the model performs.
// Lower effort reduces cost and latency.
// RawRepresentationFactory allows overriding raw OpenAI ChatCompletionOptions.
ChatClientAgent agentControllingReasoningEffort = azureOpenAiClient
    .GetChatClient("gpt-5-mini")
    .CreateAIAgent(
        options: new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                RawRepresentationFactory = _ => new ChatCompletionOptions
                {
#pragma warning disable OPENAI001
                    // Acceptable values: minimal, low, medium, high.
                    // minimal produces fastest output but least reasoning depth.
                    ReasoningEffortLevel = "minimal",
#pragma warning restore OPENAI001
                },
            }
        }
    );

// Execute the same question with reduced reasoning effort.
// Useful for cheap factual queries that do not need deep reasoning.
AgentRunResponse response2 = await agentControllingReasoningEffort.RunAsync(
    "What is the Capital of France and how many people live there?"
);

Console.WriteLine(response2);
response2.Usage.OutputAsInformation();

Utils.Separator();

// Third version uses your custom extension method.
// CreateAIAgentForAzureOpenAi abstracts the manual ChatOptions and raw factory setup.
// reasoningEffort parameter maps directly to the ReasoningEffortLevel field internally.
ChatClientAgent agentControllingReasoningEffortSimplified = azureOpenAiClient
    .GetChatClient("gpt-5-mini")
    .CreateAIAgentForAzureOpenAi(reasoningEffort: "low");

// Run the same query again with a slightly higher effort than minimal.
// low trades off speed and cost while still offering some internal reasoning.
AgentRunResponse response3 = await agentControllingReasoningEffortSimplified.RunAsync(
    "What is the Capital of France and how many people live there?"
);

Console.WriteLine(response3);
response3.Usage.OutputAsInformation();
