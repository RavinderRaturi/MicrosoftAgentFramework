using Microsoft.Extensions.AI;
using OllamaSharp;
using Shared;
using System.ComponentModel;

/// <summary>
/// Tool function exposed to the AI agent.
/// This method is discoverable and callable by the model
/// when function invocation is enabled.
/// </summary>
/// <param name="location">Location for which weather is requested</param>
/// <returns>Mock weather response for the given location</returns>
[Description("Get the weather for a given location.")]
static string GetWeather(
    [Description("The location to get the weather for.")]
    string location)
    => $"The weather in {location} is cloudy with a high of -15°C.";

/// <summary>
/// Create a chat client that connects to a locally running Ollama server.
/// The Ollama container exposes port 11434.
/// The model used here is ministral-3 running inside Docker.
/// </summary>
IChatClient chatClient =
    new OllamaApiClient(
        new Uri("http://localhost:11434"),
        "ministral-3"
    );

/// <summary>
/// Build an AI agent with:
/// - Function calling enabled
/// - A system instruction
/// - A registered tool (GetWeather)
/// </summary>
var ollamaAgent = chatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .Build()
    .CreateAIAgent(
        name: "Ollama Local-First Agent",
        instructions: "You are a helpful assistant that provides weather information.",
        tools: [
            AIFunctionFactory.Create(GetWeather)
        ]
    );

/// <summary>
/// First inference call to validate local model execution.
/// Simple text-based request.
/// </summary>
Console.WriteLine(
    await ollamaAgent.RunAsync("What is the capital of Nepal:")
);

Utils.WriteLineYellow("---- Second Call to verify Local-First ----");

/// <summary>
/// Second inference call.
/// The model should decide to invoke the GetWeather function.
/// </summary>
Console.WriteLine(
    await ollamaAgent.RunAsync("What is the weather in Nepal:")
);

/// <summary>
/// Read image file into memory for multimodal inference.
/// The image is sent as raw byte data instead of a URI,
/// which is required for local models.
/// </summary>
var byteArray = File.ReadAllBytes("image3.jpg");

/// <summary>
/// Create a multimodal chat message containing:
/// - A text prompt
/// - Binary image content
/// </summary>
ChatMessage message = new(
    ChatRole.User,
    [
        new TextContent("Describe the image"),
        new DataContent(byteArray, "image/jpg")
    ]
);

Utils.WriteLineGreen("---- Image Description Call ----");

/// <summary>
/// Run multimodal inference using the same local agent.
/// The model processes both text and image input.
/// </summary>
Console.WriteLine(
    await ollamaAgent.RunAsync(message)
);
