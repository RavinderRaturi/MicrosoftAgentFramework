using Azure.AI.OpenAI;              // Azure OpenAI SDK for working with Azure hosted OpenAI models
using Microsoft.Agents.AI;          // Microsoft Agents abstractions, including ChatClientAgent and AgentRunResponse
using Microsoft.Extensions.AI;      // Extensions for AI clients. Provides helper methods like CreateAIAgent()
using OpenAI;                       // OpenAI SDK for calling OpenAI models directly
using Shared;                       // Your shared project utilities, includes Configuration and helpers
using Shared.Extensions;            // Extension methods, including Usage.OutputAsInformation()
using System.ClientModel;           // Client model primitives like ApiKeyCredential


// Load configuration data. Typically contains API keys, endpoints, deployment names etc.
Configuration configuration = ConfigurationManager.GetConfiguration();


// Create an Azure OpenAI client that talks to your Azure OpenAI resource
AzureOpenAIClient azureOpenAIClient =
    new AzureOpenAIClient(
        new Uri(configuration.AzureOpenAiEndpoint),                         // Azure OpenAI endpoint from config
        new System.ClientModel.ApiKeyCredential(configuration.AzureOpenAiKey)); // Azure OpenAI key from config

// Create a direct OpenAI client that talks to OpenAI's public API
OpenAIClient openAIClient =
    new OpenAIClient(new ApiKeyCredential(configuration.OpenAiApiKey));     // OpenAI API key from config


// Create an AI agent for Azure OpenAI. The deployment name identifies the specific Azure chat model
ChatClientAgent azureOpenAiAgent = azureOpenAIClient
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent();

// Create an AI agent for OpenAI. Uses the same deployment name string, interpreted by OpenAI SDK
ChatClientAgent openAiAgent = openAIClient
    .GetChatClient(configuration.ChatDeploymentName)
    .CreateAIAgent();


// Select which scenario to run. Switch below determines which block of code executes
Scenario scenario = Scenario.Pdf;

//---------------------------------------------------------------------------------
// Local file paths used by the different scenarios
// Use absolute paths here. In production prefer configuration or relative paths based on AppContext.BaseDirectory
string imagePath = @"D:\Work\MicrosoftAgentFramework\AgentInputData\SampleData\image.jpg";
string pdfPath = @"D:\Work\MicrosoftAgentFramework\AgentInputData\SampleData\catan_rules.pdf";


// Holds the result from the agent after running a scenario
AgentRunResponse response;

switch (scenario)
{
    case Scenario.Text:
        {
            // Simple text only scenario against Azure OpenAI
            // Sends a single user message and prints the response and token usage
            response = await azureOpenAiAgent.RunAsync(
                new ChatMessage(ChatRole.User, "What is the capital of India?"));

            ShowResponse(response);
        }
        break;

    case Scenario.Images:
        {
            // Image analysis scenarios using Azure OpenAI

            //---------------------------------------------------------------------------------
            // Example: Image via URI  
            // Sends a prompt and an image reference by public URI
            // This demonstrates how to send remote images. It is disabled here
            //
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is in this Image?"),
                    new UriContent("https://images.unsplash.com/photo-1506744038136-46273834b3fb", "image/jpg")
                ]));
            ShowResponse(response);

            //---------------------------------------------------------------------------------
            // Image via Base64
            // 1. Read image bytes from disk
            // 2. Convert to base64
            // 3. Wrap as a data URI. Pass as DataContent with MIME type "image/jpg"
            string base64Image = Convert.ToBase64String(File.ReadAllBytes(imagePath));
            string imageDataUri = $"data:image/jpg;base64,{base64Image}";

            // Call the Azure OpenAI agent with a text prompt and the image as base64 encoded DataContent
            response = await azureOpenAiAgent.RunAsync(
                new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is in this Image?"),
                    new DataContent(imageDataUri, "image/jpg")
                ]));

            ShowResponse(response);

            //---------------------------------------------------------------------------------
            // Image via Memory
            // Alternative path. Instead of data URI, send the raw bytes as ReadOnlyMemory<byte>
            ReadOnlyMemory<byte> imageMemory = File.ReadAllBytes(imagePath).AsMemory();

            response = await azureOpenAiAgent.RunAsync(
                new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is in this Image?"),
                    new DataContent(imageMemory, "image/jpg")
                ]));

            ShowResponse(response);
        }
        break;

    case Scenario.Pdf:
        {
            // PDF analysis scenario
            // Notes
            // - This scenario only works against OpenAI in this sample. Not Azure OpenAI
            // - PDFs are sent as raw data. Not via URI

            //---------------------------------------------------------------------------------
            // PDF as Base64
            // 1. Read PDF bytes from disk
            // 2. Convert to base64
            // 3. Wrap as a data URI with MIME type "application/pdf"
            string base64Pdf = Convert.ToBase64String(File.ReadAllBytes(pdfPath));
            string pdfDataUri = $"data:application/pdf;base64,{base64Pdf}";

            // Call the OpenAI agent with a text instruction and the PDF as DataContent
            // The model is expected to parse and reason over the PDF content
            response = await openAiAgent.RunAsync(
                new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is the winning condition in attached PDF"),
                    new DataContent(pdfDataUri, "application/pdf")
                ]));

            ShowResponse(response);
            //---------------------------------------------------------------------------------
        }
        break;

    default:
        // Guard against unsupported enum values
        throw new System.NotSupportedException();
}


// Helper method to print the agent response and usage metrics to the console
void ShowResponse(AgentRunResponse agentRunResponse)
{
    // Writes the full response object. Exact formatting depends on AgentRunResponse.ToString()
    Console.WriteLine(agentRunResponse);

    // Uses an extension method to output token usage information
    agentRunResponse.Usage.OutputAsInformation();
}

// Scenario selector enum. Each value maps to a branch in the switch above
enum Scenario
{
    Text,    // Plain chat completion
    Pdf,     // PDF based reasoning using OpenAI
    Images,  // Image analysis scenarios using Azure OpenAI
}
