using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Shared;
using Workflow.AiAssisted.PizzaSample.Models;

namespace Workflow.AiAssisted.PizzaSample.Executors;

class PizzaStockCheckerExecutor() : ReflectingExecutor<PizzaStockCheckerExecutor>("StockChecker"),
    IMessageHandler<PizzaOrder, PizzaOrder>
{
    public ValueTask<PizzaOrder> HandleAsync(PizzaOrder message, IWorkflowContext context, CancellationToken
        cancellationToken)
    {
        foreach (string topping in message.Toppings)
        {
            if (topping.ToLower() == "Pineapaaaaple".ToLower()) // Out of stock sample. 
            {
                Utils.WriteLineDarkGray($"--- Add out of stock wrning: {topping}");
                message.Warnings.Add(WarningType.OutOfIngredient.ToString(), topping);
            }
            else
            {
                Utils.WriteLineYellow($"- Add {topping} onto Pizza (Reduced sotck)");
            }
        }
        return ValueTask.FromResult(message);
    }
}