using JetBrains.Annotations;


namespace Workflow.AiAssisted.PizzaSample.Models;





enum WarningType { OutOfIngredient }

[PublicAPI]
enum PizzaSize { Small, Medium, Large }



[PublicAPI]
class PizzaOrder
{
    public PizzaSize Size { get; set; }
    public List<string> Toppings { get; set; } = new();
    public Dictionary<string, object> Warnings { get; set; } = new();
}


