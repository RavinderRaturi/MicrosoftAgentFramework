using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.ClientModel;

// Load configuration settings for Azure OpenAI and other app level values.
Configuration configuration = ConfigurationManager.GetConfiguration();

// Create the Azure OpenAI client using endpoint and API key from configuration.
// This client will be used to create chat clients and agents.
AzureOpenAIClient client = new(
    new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey));

// Create an "allocator" agent that is responsible for routing user requests
// to the correct specialist agent instead of answering questions directly.
ChatClientAgent allocatorAgent = client
    .GetChatClient(configuration.ChatDeploymentName4)
    .CreateAIAgent(
        name: "AllocatorAgent",
        instructions: "You are an agent that allocates tasks to specialized agents based on user requests. Do not answer your self");

// Create a history specialist agent that focuses on history related questions.
ChatClientAgent historytAgent = client
    .GetChatClient(configuration.ChatDeploymentName4)
    .CreateAIAgent(
        name: "HistorySpecialistAgent",
        instructions: "You are an agent that specializes in handling history related questions. You are an History Nerd");

// Create a biology specialist agent that focuses on biology related questions.
ChatClientAgent scienceAgent = client
    .GetChatClient(configuration.ChatDeploymentName4)
    .CreateAIAgent(
        name: "BiologySpecialistAgent",
        instructions: "You are an agent that specializes in handling biology related questions. You are a Biology Geek");

// Main console loop to continuously accept user questions until "exit" is typed.
while (true)
{
    // Conversation messages for this single turn.
    // This is re created per question, so there is no long term conversation memory.
    List<ChatMessage> messages = [];

    Console.WriteLine("Enter your question (or 'exit' to quit):");
    Console.Write("> ");
    string userInput = Console.ReadLine() ?? string.Empty;

    // Terminate the loop if the user wants to exit.
    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    // Build a workflow that starts with the allocator agent.
    // The allocator can hand off requests to either the history or biology agents.
    // The history and biology agents can then hand the result back to the allocator.
    Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(allocatorAgent)
        // Allow allocatorAgent to hand off to the history or biology specialist agents.
        .WithHandoffs(allocatorAgent, [historytAgent, scienceAgent])
        // Allow the specialists to hand the response back to the allocator for finalization.
        .WithHandoffs([historytAgent, scienceAgent], allocatorAgent, "Handing the response to Allocator Agent")
        .Build();

    // Seed the workflow with the user message as the first chat message.
    messages.Add(new ChatMessage(ChatRole.User, userInput));

    // Execute the workflow and stream results, then collect the final messages.
    messages.AddRange(await RunWorkflowAsync(workflow, messages));
}

// Execute the workflow and stream intermediate events to the console.
// Returns the final list of ChatMessage objects produced by the workflow.
static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
{
    // Tracks which executor (agent) produced the last output segment.
    // Used to print agent headers only when the active executor changes.
    string? lastExecutorId = null;

    // Log which executor the workflow starts with, mainly for debugging.
    Utils.WriteLineRed(workflow.StartExecutorId.ToString());

    // Start an in process streaming execution of the workflow using the initial messages.
    StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);

    // Send a turn token to trigger the workflow to process the input and emit events.
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

    // Listen for workflow events as they are streamed from the execution.
    await foreach (WorkflowEvent @event in run.WatchStreamAsync())
    {
        switch (@event)
        {
            // AgentRunUpdateEvent: an agent is producing incremental output (tokens, content, or tool calls).
            case AgentRunUpdateEvent e:
                {
                    // If a different executor is now active, print a new header line.
                    if (e.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = e.ExecutorId;
                        Console.WriteLine();
                        // Print the agent name or executor id in green for clarity.
                        Utils.WriteLineGreen(e.Update.AuthorName ?? e.ExecutorId);
                    }

                    // Stream the text content to the console without a trailing newline
                    // so that tokens can appear as they arrive.
                    Console.Write(e.Update.Text);

                    // If the model invoked a function, log the function name and arguments in yellow.
                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent functionCall)
                    {
                        Console.WriteLine();
                        Utils.WriteLineYellow(
                            $"[Function Call: {functionCall.Name}({string.Join(", with arguments: ", functionCall.Arguments.Select(kv => kv.Key + "=" + kv.Value))})]");
                    }

                    break;
                }

            // WorkflowOutputEvent: the workflow has completed and produced a final output payload.
            case WorkflowOutputEvent output:
                {
                    // Print a visual separator before returning.
                    Utils.Separator();

                    // Convert the workflow output into a strongly typed list of ChatMessage and return it.
                    return output.As<List<ChatMessage>>()!;
                }

            // ExecutorFailedEvent: an agent or step failed during execution.
            case ExecutorFailedEvent failedEvent:
                if (failedEvent.Data is Exception ex)
                {
                    // Log detailed exception information if available.
                    Utils.WriteLineRed($"Executor {failedEvent.ExecutorId} failed with exception: {ex.Message}");
                }
                else
                {
                    // Fallback in case no exception object is attached.
                    Utils.WriteLineRed($"Executor {failedEvent.ExecutorId} failed with unknown error.");
                }
                break;
        }
    }

    // If the stream ends without a WorkflowOutputEvent, return an empty list of messages.
    return [];
}
