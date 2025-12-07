using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

// Alias added to avoid ambiguity between multiple ChatMessage types
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

// ------------------------------------------------------------
// Load application configuration settings
// ------------------------------------------------------------
Configuration configuration = ConfigurationManager.GetConfiguration();

// ------------------------------------------------------------
// Initialize Azure OpenAI client with endpoint and API key
// ------------------------------------------------------------
AzureOpenAIClient client = new AzureOpenAIClient(
    new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey)
);

// ------------------------------------------------------------
// Obtain the ChatClient tied to the specific deployment
// ------------------------------------------------------------
ChatClient chatClient = client.GetChatClient(configuration.ChatDeploymentName4);

// ------------------------------------------------------------
// Create AI agents with focused responsibilities
// ------------------------------------------------------------
ChatClientAgent legalAgent = chatClient.CreateAIAgent(
    name: "LegalAgent",
    instructions: "You are an legal agent and your job is to see if the text is legal.Use max 200 chars"
);

ChatClientAgent spellCheck = chatClient.CreateAIAgent(
    name: "SpellCheckAgent",
    instructions: "You are a spell check agent and your job is to correct any spelling mistakes in the text.Use max 200 chars"
);

// ------------------------------------------------------------
// Build a concurrent workflow so both agents run in parallel
// ------------------------------------------------------------
Workflow workflow = AgentWorkflowBuilder.BuildConcurrent(new[] { legalAgent, spellCheck });

// ------------------------------------------------------------
// Sample legal text to be processed by the agents
// ------------------------------------------------------------
string legalText = """
                   This Legal Disclaimer (“Agreement”) governs the ownership, maintenance, and care of domesticated ducks 
                   kept as personal pets. By acquiring or housing a duck, the Owner hereby acknowledges and agrees to 
                   comply with all applicable municipal and federal regulations concerning the keeping of live poultry. 
                   The Owner affirms responsibility for providing humane living conditions, including adequate shelter, 
                   food, and access to clean water. Ducks must not be subjected to neglect, cruelty, or abandonment.
                   The Owner shall maintain sanitary standards to prevent odors, noise disturbance, or the spread of 
                   disease to neighboring properties. Local authorities reserve the right to inspect premises upon 
                   reasonable notice to ensure compliance. Any sale or transfer of pet ducks must include written 
                   documentation verifying the animal’s health status and vaccination records where required.
                   This Agreement does not confer any breeding or commercial rights unless expressly authorized in 
                   writing by the relevant agency. The Owner indemnifies and holds harmless all regulatory bodies 
                   against claims arising from damage or injury caused by said animals. Failure to adhere to the 
                   provisions herein may result in fines, forfeiture, or legal action.
                   Acceptance of a duck as a pet constitutes full consent to these terms and any subsequent 
                   amendmants or revisions adopted by the governing authority.
                   """;

// ------------------------------------------------------------
// Wrap the input text inside a user ChatMessage for the workflow
// ------------------------------------------------------------
var message = new List<ChatMessage>
{
    new(ChatRole.User, legalText)
};

// ------------------------------------------------------------
// Start streaming execution of the workflow
// ------------------------------------------------------------
StreamingRun run = await InProcessExecution.StreamAsync(workflow, message);

// ------------------------------------------------------------
// Notify the workflow that no further input messages will be sent
// ------------------------------------------------------------
await run.TrySendMessageAsync(new TurnToken(true));

// ------------------------------------------------------------
// Prepare container for the agent responses
// ------------------------------------------------------------
List<ChatMessage> responses = new();

// ------------------------------------------------------------
// Listen for workflow output events and capture final result
// ------------------------------------------------------------
await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
{
    if (evt is WorkflowOutputEvent complete)
    {
        responses = (List<ChatMessage>)complete.Data;
        break;
    }
}

// ------------------------------------------------------------
// Print each agent response with role-based formatting
// ------------------------------------------------------------
foreach (ChatMessage msg in responses)
{
    if (msg.Role == ChatRole.User)
    {
        Utils.WriteLineYellow(msg.AuthorName ?? "UnKnown");
        Console.WriteLine($"{msg.Text}");
        Utils.Separator();
    }

    Utils.WriteLineGreen(msg.AuthorName ?? "UnKnown");
    Console.WriteLine($"{msg.Text}");
    Utils.Separator();
}
