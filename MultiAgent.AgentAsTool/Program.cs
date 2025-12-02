using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgent.AgentAsTool;
using OpenAI;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using System.Text;

// Load configuration (endpoint, keys, deployment names, etc.).
// Assumes ConfigurationManager.GetConfiguration() is your own helper.
Configuration configuration = ConfigurationManager.GetConfiguration();

// Create a single AzureOpenAIClient that will be used to create agents and chat clients.
AzureOpenAIClient client = new(
    new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey));

#region String tools agent

// Create an AI agent specialized for string manipulation.
// This agent is given three tools: Reverse, UpperCase, LowerCase.
// The tools are created from static methods on StringTools via AIFunctionFactory.Create.
// The agent is then wrapped in a builder pipeline to attach middleware that logs tool calls.
AIAgent stringAgent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
        name: "StringToolsAgent",
        instructions: "You are string manipulator",
        tools:
        [
            // Each of these is turned into a callable tool that the model can function call.
            AIFunctionFactory.Create(StringTools.Reverse),
            AIFunctionFactory.Create(StringTools.UpperCase),
            AIFunctionFactory.Create(StringTools.LowerCase),
        ])
    .AsBuilder()
    // Attach middleware that wraps every tool invocation, logs it, then calls the next handler.
    .Use(FunctionCallMiddleware)
    .Build();

#endregion

#region Number tools agent

// Create an AI agent specialized for number operations.
// This agent exposes number related tools, for example returning the "answer to every problem"
// and generating a random number.
AIAgent numberAgent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
        name: "NumberToolsAgent",
        instructions: "You are number expert",
        tools:
        [
            AIFunctionFactory.Create(NumberTools.AnswerToEveryProblem),
            AIFunctionFactory.Create(NumberTools.RandomNumber),
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

#endregion

// Visual separator in console so the user can see which sample is running.
Utils.WriteLineGreen("DELEGATE AGENT AS TOOL SAMPLE");

#region Delegation agent . agent as tool

// This agent does not do any string or number work itself.
// Instead, it has two tools, and each tool is actually an entire agent.
//
// stringAgent is wrapped as a tool by calling AsAIFunction.
// numberAgent is also wrapped as a tool.
// The DelegationAgent decides which agent tool to call for a given user request.
AIAgent delegationAgent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
        name: "DelegationAgent",
        instructions: "Are a Delegator of String and Number Tasks. Never does such work yourself",
        tools:
        [
            // Wrap stringAgent as a single tool named "StringToolsAgent".
            // From the model perspective this looks like a function tool.
            stringAgent.AsAIFunction(new AIFunctionFactoryOptions
            {
                Name = "StringToolsAgent",
            }),
            // Wrap numberAgent as a single tool named "NumberToolsAgent".
            numberAgent.AsAIFunction(new AIFunctionFactoryOptions
            {
                Name = "NumberToolsAgent",
            }),
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

// Run a sample query against the DelegationAgent.
// The agent should:
// 1. Delegate the uppercasing part to StringToolsAgent.
// 2. Delegate random number generation to NumberToolsAgent.
AgentRunResponse responseFromDelegate =
    await delegationAgent.RunAsync("Upper case 'hello world' and give me a random number.");

// Dump the full response to the console.
Console.WriteLine(responseFromDelegate);

// Use custom extension method to print token and cost usage.
responseFromDelegate.Usage.OutputAsInformation();

#endregion

// Console separator for clarity.
Utils.Separator();

Utils.WriteLineGreen("DIRECT AGENT CALL SAMPLE");

#region Direct agent call . tools on a single agent

// In this sample, instead of delegates to other agents, we create a single agent that directly
// owns all tools. The model decides which tool to call for each part of the task.
// There is no agent as tool wrapping here, just plain function tools.
AIAgent directAgentCall = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
        name: "DirectAgentCall",
        instructions: "You are an agent that can call other agents directly based on whether the task is string or number manipulation.",
        tools:
        [
            // String tools
            AIFunctionFactory.Create(StringTools.Reverse),
            AIFunctionFactory.Create(StringTools.UpperCase),
            AIFunctionFactory.Create(StringTools.LowerCase),

            // Number tools
            AIFunctionFactory.Create(NumberTools.AnswerToEveryProblem),
            AIFunctionFactory.Create(NumberTools.RandomNumber),
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

// Run a request that needs both string and number operations.
// 1. Lower case "HELLO WORLD".
// 2. Get "the answer to every problem".
AgentRunResponse responseFromDirectCall =
    await directAgentCall.RunAsync("Upper case 'hello world' and give me a random number.");

// Print the full model response.
Console.WriteLine(responseFromDirectCall);

// Print usage information again.
responseFromDirectCall.Usage.OutputAsInformation();

#endregion

#region Middleware for tool call logging

// FunctionCallMiddleware is injected into each agent pipeline via .Use(FunctionCallMiddleware).
// It executes for every tool call that the agent makes.
// Its job here is to log which tool is being called, which agent is calling it,
// and what arguments are being passed.
async ValueTask<object?> FunctionCallMiddleware(
    AIAgent callingAgent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();

    // Log the function name and the logical agent that is calling it.
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}' [Agent:{callingAgent.Name}]");

    // If the tool has arguments, print them all.
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append(" with arguments:");
        foreach (var arg in context.Arguments)
        {
            // Example format.
            // with arguments: text='HELLO' (Args : text)
            functionCallDetails.Append($" {arg.Key}='{arg.Value}'");
        }

        // Also write a simple list of argument names at the end.
        functionCallDetails.Append($" (Args : {string.Join(", ", context.Arguments.Select(kv => kv.Key))})");
    }

    // Use a custom helper to write log lines in a different color for visibility.
    Utils.WriteLineDarkGray(functionCallDetails.ToString());

    // Important. Call the next middleware or the actual tool implementation.
    // This keeps the pipeline flowing and ensures the tool actually runs.
    return await next(context, cancellationToken);
}

#endregion
