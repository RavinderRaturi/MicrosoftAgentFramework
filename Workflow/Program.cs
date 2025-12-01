


using Microsoft.Agents.AI.Workflows;
using Shared;
using Workflow.AiAssisted.PizzaSample;
using Workflow.AiAssisted.PizzaSample.Executors;
using Workflow.AiAssisted.PizzaSample.Models;

Configuration configuration = ConfigurationManager.GetConfiguration();
AgentFactory agentFactory = new(configuration);

PizzaOrderParserExecutor orderParserExecutor = new(agentFactory.CreateOrderTakerAgent());
PizzaStockCheckerExecutor stockCheckerExecutor = new();
PizzaSuccessExecutor successExecutor = new();
PizzaWarningExecutor warningExecutor = new(agentFactory.CreateWarningToCustomerAgent());


WorkflowBuilder builder = new(orderParserExecutor);

builder.AddEdge(
    source: orderParserExecutor,
    target: stockCheckerExecutor
    );

builder.AddSwitch(
    source: stockCheckerExecutor,
    switchBuilder =>
        {
            switchBuilder.AddCase<PizzaOrder>(x => x!.Warnings.Count == 0, successExecutor);
            switchBuilder.AddCase<PizzaOrder>(x => x!.Warnings.Count != 0, warningExecutor);
        }
    );

Microsoft.Agents.AI.Workflows.Workflow workflow = builder.Build();

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string input = "I would like to order a large pizza with pepperoni, mushrooms, and pineapple.";

StreamingRun run = await InProcessExecution.StreamAsync(workflow: workflow, input: input);

await foreach (var output in run.WatchStreamAsync())
{
    if (output is ExecutorCompletedEvent executorCompleted)
    {
        Utils.WriteLineDarkGray($"--- Executor Completed: {executorCompleted.ExecutorId}");
    }

}