using JetBrains.Annotations;


namespace Workflow.AiAssisted.PizzaSample.Models;





enum WarningType { OutOfIngredient }

[PublicAPI]
enum PizzaSize { Small, Medium, Large }



[PublicAPI]
class PizzaOrder
{
    public PizzaSize Size { get; set; }
    public List<string> Toppings { get; set; } = [];
    public Dictionary<WarningType, string> Warnings { get; set; } = [];
}