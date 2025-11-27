
// Import Azure OpenAI SDK, the Agents SDKs, Shared utilities, your model classes, and JSON libraries.
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using StronglyTypedOutputStructuredOutput.Models;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

// Load configuration values such as Azure OpenAI endpoint and API key.
// This typically reads from appsettings.json or environment variables.
Configuration configuration = ConfigurationManager.GetConfiguration();

// Create Azure OpenAI client that will be used by all agents.
AzureOpenAIClient client = new(
    new Uri(configuration.AzureOpenAiEndpoint),
    new ApiKeyCredential(configuration.AzureOpenAiKey)
);

// The question we want to ask the AI model.
string question = "What are the top 10 Movies according to IMDB?";


// ---------------------------------------------------------------------------
// 1. NO STRUCTURED OUTPUT
// ---------------------------------------------------------------------------
// This approach returns plain text only. No guarantees about formatting.
// If the AI gives malformed JSON or inconsistent structure, you are on your own.

AIAgent agent1 = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(instructions: "You are an expert in IMDB Lists");

// Send question to agent1 and get raw, unstructured text.
AgentRunResponse response1 = await agent1.RunAsync(question);

// Print raw response. No structure. You might get text or JSON or bullet points.
Console.WriteLine(response1);

Utils.Separator();


// ---------------------------------------------------------------------------
// 2. STRUCTURED OUTPUT USING ChatClientAgent
// ---------------------------------------------------------------------------
// This is the cleaner and recommended method when your output can be mapped
// directly to a C# model class like MovieResult.
// The agent internally handles strong typing. You just specify <MovieResult>.

// Notice: This is ChatClientAgent meaning it supports generic typed responses.
ChatClientAgent agent2 = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(instructions: "You are an expert in IMDB Lists");

// Request a strongly typed MovieResult using generics.
AgentRunResponse<MovieResult> response2 = await agent2.RunAsync<MovieResult>(question);

// Get the C# object directly instead of raw text.
MovieResult movieResult2 = response2.Result;

// Display parsed movies. No JSON parsing needed. It is already a typed object.
DisplayMovies(movieResult2);

Utils.Separator();


// ---------------------------------------------------------------------------
// 3. MANUAL JSON DESERIALIZATION USING JSON SCHEMA
// ---------------------------------------------------------------------------
// This approach is more verbose but needed when:
//   - You need full control over JSON rules
//   - Your JSON field names differ from your C# fields
//   - You require custom converters or complex mapping
//   - The model generates inconsistent naming patterns

// These options configure how JSON is interpreted during deserialization.
JsonSerializerOptions jsonSerializerOptions = new()
{
    PropertyNameCaseInsensitive = true,  // JSON fields can be any casing. C# properties still match.
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(), // Helps serializer understand type metadata.
    Converters = { new JsonStringEnumConverter() } // Converts enum values as strings instead of integers.
};

// Create a standard AIAgent again. No generic type support here.
AIAgent agent3 = client
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent(instructions: "You are an expert in IMDB Lists");

// Force response to follow a JSON schema derived from MovieResult + serializer options.
// This ensures the model must return valid JSON that fits MovieResult structure.
ChatResponseFormatJson chatResponseFormatJson =
    ChatResponseFormat.ForJsonSchema<MovieResult>(jsonSerializerOptions);

// Ask model for JSON formatted exactly as MovieResult.
AgentRunResponse response3 = await agent3.RunAsync(
    question,
    options: new ChatClientAgentRunOptions()
    {
        ChatOptions = new ChatOptions
        {
            ResponseFormat = chatResponseFormatJson  // Enforce JSON schema response
        }
    }
);

// Deserialize raw JSON response manually using previously defined options.
MovieResult movieResult3 =
    response3.Deserialize<MovieResult>(jsonSerializerOptions);

// Display results. Same display logic as before.
DisplayMovies(movieResult3);


// ---------------------------------------------------------------------------
// Display Method
// ---------------------------------------------------------------------------
// Prints each movie with index, title, year, genre, director, and IMDB score.

void DisplayMovies(MovieResult movieResult)
{
    int counter = 1;

    // Print the message that the AI included, for example: "Here are the top movies".
    Console.WriteLine(movieResult.MessageBack);

    // Loop through Top10Movies array and format the output.
    foreach (Movie movie in movieResult.Top10Movies)
    {
        Console.WriteLine(
            $"{counter}: {movie.Title} ({movie.YearOfRelease}) - " +
            $"Genre: {movie.Genre} - Director: {movie.Director} - IMDB Score: {movie.ImdbScore}"
        );
        counter++;
    }
}
