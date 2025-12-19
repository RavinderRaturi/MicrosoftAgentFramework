
using Microsoft.Extensions.AI;
using OllamaSharp;




IChatClient chatClient =
    new OllamaApiClient(new Uri("http://localhost:11434"), "ministral-3");

var ollamaAgent = chatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .Build()
    .CreateAIAgent(
    name: "Ollama Local-First Agent",
    instructions: "You are a helpful assistant that provides weather information."
     );

