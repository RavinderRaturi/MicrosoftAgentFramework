namespace MultiAgent.AgentAsTool;

internal static class StringTools
{
    public static string UpperCase(string str) => str.ToUpper();
    public static string LowerCase(string str) => str.ToLower();
    public static string Reverse(string str) => new string(str.ToCharArray().Reverse().ToArray());
}



internal static class NumberTools
{
    public static int RandomNumber() => Random.Shared.Next();
    public static int AnswerToEveryProblem() => 99;
}
