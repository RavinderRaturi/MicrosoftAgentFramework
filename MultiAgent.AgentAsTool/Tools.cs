namespace MultiAgent.AgentAsTool;

#region String tools

// Static container class for all string-related functions.
// These methods are exposed as AI tools using AIFunctionFactory.Create(...)
// Each public static method becomes one callable function
// that the agent can invoke via function-calling.
internal static class StringTools
{
    // Converts the input string to uppercase.
    // Used by the StringToolsAgent and callable by the model.
    public static string UpperCase(string str)
        => str.ToUpper();

    // Converts the input string to lowercase.
    // Used by the StringToolsAgent and callable by the model.
    public static string LowerCase(string str)
        => str.ToLower();

    // Reverses the input string.
    // Implementation uses LINQ to convert the string to a char array,
    // reverse the array, and reconstruct the final string.
    public static string Reverse(string str)
        => new string(str.ToCharArray().Reverse().ToArray());
}

#endregion


#region Number tools

// Static container class for all number-related functions.
// These methods are also exposed as AI tools and used by the NumberToolsAgent.
internal static class NumberTools
{
    // Returns a random integer using the shared Random instance.
    // The result is non-deterministic and differs on each call.
    // Called by the agent when the user requests a random number.
    public static int RandomNumber()
        => Random.Shared.Next();

    // Returns a constant value representing the humorous
    // "answer to every problem".
    // This tool is deterministic and always returns the same number.
    public static int AnswerToEveryProblem()
        => 99;
}

#endregion
