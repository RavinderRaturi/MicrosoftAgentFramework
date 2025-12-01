using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Shared;
using System.Text;
using Workflow.AiAssisted.PizzaSample.Models;

namespace Workflow.AiAssisted.PizzaSample.Executors;

class PizzaWarningExecutor(ChatClientAgent warningToCustomerAgent) : ReflectingExecutor<PizzaWarningExecutor>("PizzaWarning"), IMessageHandler<PizzaOrder>
{
    public async ValueTask HandleAsync(PizzaOrder message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        Utils.WriteLineRed("Can't create the pizza in full");

        StringBuilder sb = new();

        foreach (KeyValuePair<string, object> warning in message.Warnings)
        {
            string value = warning.Value.ToString();
            if (warning.Value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                value = string.Join(", ", element.EnumerateArray().Select(x => x.ToString()));
            }
            sb.AppendLine($" - {warning.Key}: {value}");
        }

        AgentRunResponse response = await warningToCustomerAgent.RunAsync($"Explain to the use can't we can't for-fill their order do to the following: {sb}", cancellationToken: cancellationToken);
        Console.WriteLine("Send as email: " + response);
    }
}