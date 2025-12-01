using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgent.AgentAsTool;
using OpenAI;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using System.Text;

Configuration configuration = ConfigurationManager.GetConfiguration();

AzureOpenAIClient client = new(new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey));


AIAgent stringAgent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(

    name: "StringToolsAgent",
        instructions: "You are string manipulator",
        tools: [
            AIFunctionFactory.Create(StringTools.Reverse),
            AIFunctionFactory.Create(StringTools.UpperCase),
            AIFunctionFactory.Create(StringTools.LowerCase)
            ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();





async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
       Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}' [Agent:{callingAgent.Name}]");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append(" with arguments:");
        foreach (var arg in context.Arguments)
        {
            functionCallDetails.Append($" {arg.Key}='{arg.Value}'");
        }
        functionCallDetails.Append($" (Args : {string.Join(", ", context.Arguments.Select(kv => kv.Key))})");
    }
    Utils.WriteLineDarkGray(functionCallDetails.ToString());
    return await next(context, cancellationToken);
}