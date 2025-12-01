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

AIAgent numberAgent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
        name: "NumberToolsAgent",
        instructions: "You are number manipulator",
        tools: [
            AIFunctionFactory.Create(NumberTools.AnswerToEveryProblem),
            AIFunctionFactory.Create(NumberTools.RandomNumber)
            ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

Utils.WriteLineGreen("DELEGATE AGENT AS TOOL SAMPLE");

AIAgent delegationAgent = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(
        name: "DelegationAgent",
        instructions: "You are an agent that delegates tasks to other agents based on whether the task is string or number manipulation.",
        tools: [
stringAgent.AsAIFunction(new AIFunctionFactoryOptions
{
    Name = "StringToolsAgent",
}),
numberAgent.AsAIFunction(new AIFunctionFactoryOptions
{
    Name= "NumberToolsAgent"
})
            ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AgentRunResponse responseFromDelegate = await delegationAgent.RunAsync("Upper case 'hello world' and get me a random number");
Console.WriteLine(responseFromDelegate);
responseFromDelegate.Usage.OutputAsInformation();

Utils.Separator();


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