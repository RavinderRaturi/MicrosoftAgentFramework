
using Microsoft.Extensions.AI;
using OllamaSharp;
using Shared;


using System.ComponentModel;

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of -15°C.";





IChatClient chatClient =
    new OllamaApiClient(new Uri("http://localhost:11434"), "ministral-3");

var ollamaAgent = chatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .Build()
    .CreateAIAgent(
    name: "Ollama Local-First Agent",
    instructions: "You are a helpful assistant that provides weather information.",
    tools:
     [AIFunctionFactory.Create(GetWeather)]
     );




Console.WriteLine(
    await ollamaAgent.RunAsync("What is the capital of Nepal:")
    );

Utils.WriteLineYellow("---- Second Call to verify Local-First ----");


Console.WriteLine(
    await ollamaAgent.RunAsync("What is the weather in Nepal:")
    );

