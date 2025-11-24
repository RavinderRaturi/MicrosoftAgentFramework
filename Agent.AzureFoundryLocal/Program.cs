using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using OpenAI;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // All user-facing status messages are Yellow.
        // All AI responses are Cyan.
        // The console color is reset after each write to avoid bleed.

        #region Check if Foundry Local is installed

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Diagnostics. Checking for Foundry Local runtime. Querying winget.");
        Console.ResetColor();

        string packageId = "Microsoft.FoundryLocal";

        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = $"list --id={packageId}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        bool isFoundryInstalled = output.Contains(packageId, StringComparison.OrdinalIgnoreCase);

        if (isFoundryInstalled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Foundry Local detected. Proceeding.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Foundry Local not found. System is not ready. Preparing installation.");
            Console.ResetColor();
        }

        #endregion

        #region Install Part (if needed)

        if (!isFoundryInstalled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Starting silent install of Foundry Local. Expect system load and no UI feedback.");
            Console.WriteLine("If the install hangs, check winget logs. Do not interrupt.");
            Console.ResetColor();

            Process installProcess = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install Microsoft.FoundryLocal --accept-package-agreements --accept-source-agreements --silent",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            installProcess.Start();
            installProcess.WaitForExit();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Installation phase complete. Moving to model startup.");
            Console.ResetColor();
        }

        #endregion

        #region Start Foundry and download model if needed

        string modelAlias = "qwen2.5-coder-0.5b";

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Initializing model '{modelAlias}'. If this is the first run, download time will be significant.");
        Console.WriteLine("Foundry Local is powering up the runtime. Stand by for memory allocation.");
        Console.ResetColor();

        FoundryLocalManager manager = await FoundryLocalManager.StartModelAsync(modelAlias);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Validating model registration and pulling metadata.");
        Console.ResetColor();

        ModelInfo? modelInfo = await manager.GetModelInfoAsync(modelAlias);

        if (modelInfo == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Model metadata unavailable. This indicates a startup failure. Abort.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Model '{modelInfo.ModelId}' operational. Endpoint assigned: {manager.Endpoint}");
        Console.ResetColor();

        #endregion

        #region Create client and agent

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Configuring OpenAI compatible client for local inference.");
        Console.WriteLine("Authenticating with placeholder API key. Local models ignore credential but require the object.");
        Console.ResetColor();

        OpenAIClient client = new(new ApiKeyCredential("NO_API_KEY"), new OpenAIClientOptions
        {
            Endpoint = manager.Endpoint
        });

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Binding chat agent to the loaded model. Preparing execution pipeline.");
        Console.ResetColor();

        ChatClientAgent agent = client.GetChatClient(modelInfo.ModelId).CreateAIAgent();

        #endregion

        #region Non streaming run

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Running first request. Non streaming mode. Full response expected in one block.");
        Console.ResetColor();

        AgentRunResponse response = await agent.RunAsync("What comes after Five?");

        // AI response printed in Cyan
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Response received:");
        Console.WriteLine(response);
        Console.ResetColor();

        #endregion

        #region Streaming run

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Switching to streaming mode. Tokens will arrive incrementally.");
        Console.WriteLine("If nothing appears for a moment, model is generating. This is expected.");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(@"
 ~~~  ~~~   ~~~~~  ~~~~~~~   ~~~~
   ~  STREAMING  ~
 ~~~~   ~~~~~  ~~~   ~~~~~    ~~~
");
        Console.ResetColor();

        // Streamed AI output chunks in Cyan. Status / control messages remain Yellow.
        await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync("Tell me a story of a sea monster"))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(update);
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nStreaming complete. Model idle. Session finished.");
        Console.ResetColor();

        #endregion
    }
}
