using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

/// <summary>
/// Extension helpers that create ChatClientAgent instances from an OpenAI <see cref="ChatClient"/>.
/// </summary>
namespace MicrosoftAgentFramework.Utilities.Extensions
{
    [PublicAPI]
    public static class ChatClientExtensions
    {
        /// <summary>
        /// Creates an AI agent from an <see cref="ChatClient"/> using the OpenAI Chat Completion API.
        /// This method is a convenience alias that forwards to <see cref="CreateAIAgentForAzureOpenAi"/>.
        /// Use this overload when you want the same behavior but prefer the "OpenAi" naming.
        /// </summary>
        /// <param name="client">The OpenAI <see cref="ChatClient"/> to use for the agent. This must not be null.</param>
        /// <param name="instructions">Optional system instructions that define the agent's default behavior and personality.
        /// These instructions are used to initialize the agent's system message or its equivalent in the agent's chat options.
        /// Provide concise, authoritative instructions to control global behavior that should persist across conversations.</param>
        /// <param name="name">Optional human readable name for the agent. Used for identification in logs and diagnostics.</param>
        /// <param name="description">Optional free text describing the agent's capabilities and intended purpose.
        /// Helpful for observability and remote debugging so operators can understand the agent's intended role.</param>
        /// <param name="tools">Optional collection of <see cref="AITool"/> instances the agent can call.
        /// Tools represent external actions or integrations the agent can invoke during a conversation. If null or empty, the agent will have no tool capabilities.</param>
        /// <param name="reasoningEffort">If the underlying model supports a reasoning mode, this maps to that model's reasoning effort.
        /// Valid values are 'minimal', 'low', 'medium', and 'high'. Use 'minimal' for fast, low-cost factual responses.
        /// Use 'high' when you expect complex multi-step reasoning. This is optional and only applied when not null or whitespace.</param>
        /// <param name="clientFactory">Optional callback to wrap or replace the resolved <see cref="IChatClient"/> implementation.
        /// Useful to add message interceptors, metrics, testing doubles, or request decorators. The provided <see cref="IChatClient"/> is the default client derived from the SDK <see cref="ChatClient"/>.</param>
        /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/> enabling the created agent and underlying components to log diagnostic information.</param>
        /// <param name="services">Optional <see cref="IServiceProvider"/> used to resolve dependencies for any <see cref="AIFunction"/> or tool implementations that require DI.</param>
        /// <returns>A configured <see cref="ChatClientAgent"/> that uses the provided chat client and options.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is null.</exception>
        // ReSharper disable once InconsistentNaming
        public static ChatClientAgent CreateAIAgentForOpenAi(
            this ChatClient client,
            string? instructions = null,
            string? name = null,
            string? description = null,
            IList<AITool>? tools = null,
            string? reasoningEffort = null,
            Func<IChatClient, IChatClient>? clientFactory = null,
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? services = null)
            => CreateAIAgentForAzureOpenAi(client, instructions, name, description, tools, reasoningEffort, clientFactory, loggerFactory, services);

        /// <summary>
        /// Creates an AI agent from an <see cref="ChatClient"/> using the OpenAI Chat Completion API.
        /// This method constructs the agent configuration and returns a ready to use <see cref="ChatClientAgent"/>.
        /// It performs light mapping from the supplied parameters into <see cref="ChatOptions"/> and
        /// the agent options used by <see cref="ChatClientAgent"/>. This is the implementation target for both public overloads.
        /// </summary>
        /// <param name="client">The OpenAI <see cref="ChatClient"/> instance to create the agent from. Required.</param>
        /// <param name="instructions">Optional system instructions to seed the agent's system message or equivalent policy.</param>
        /// <param name="name">Optional name used for identification and logging.</param>
        /// <param name="description">Optional description explaining the agent's intended responsibilities.</param>
        /// <param name="tools">Optional list of tools the agent may call. Each tool should implement the <see cref="AITool"/> contract expected by the agent framework.</param>
        /// <param name="reasoningEffort">Optional mapping to the underlying model's reasoning effort level.
        /// If provided, we attach a RawRepresentationFactory to the ChatOptions that sets the model specific ReasoningEffortLevel field.</param>
        /// <param name="clientFactory">Optional factory to transform or replace the resolved <see cref="IChatClient"/> prior to agent creation.</param>
        /// <param name="loggerFactory">Optional logger factory used by the created <see cref="ChatClientAgent"/> for structured logging.</param>
        /// <param name="services">Optional service provider used by tool or function invocations requiring dependency injection.</param>
        /// <returns>A configured <see cref="ChatClientAgent"/> instance wired to the provided chat client and options.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is null.</exception>
        // ReSharper disable once InconsistentNaming
        public static ChatClientAgent CreateAIAgentForAzureOpenAi(
            this ChatClient client,
            string? instructions = null,
            string? name = null,
            string? description = null,
            IList<AITool>? tools = null,
            string? reasoningEffort = null,
            Func<IChatClient, IChatClient>? clientFactory = null,
            ILoggerFactory? loggerFactory = null,
            IServiceProvider? services = null)
        {
            // Defensive validation. The original snippet did not explicitly check for a null ChatClient parameter.
            // Throwing early makes errors easier to debug and prevents null dereferences downstream.
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            // Create a high level ChatOptions object that the agent framework expects.
            // ChatOptions encapsulates instructions, tools, and a factory to produce the raw provider specific options.
            ChatOptions options = new();

            // If a reasoning effort string was provided, map it to the provider specific low level object.
            // We do this by setting RawRepresentationFactory which will be invoked by the agent framework
            // to obtain the provider specific ChatCompletionOptions when making a call.
            // This keeps the provider specific fields out of the cross provider ChatOptions type.
            if (!string.IsNullOrWhiteSpace(reasoningEffort))
            {
                // RawRepresentationFactory is a function that returns the provider specific options object.
                // Here we construct a ChatCompletionOptions and set its ReasoningEffortLevel to the requested value.
                // Note. The ReasoningEffortLevel is provider specific. The agent framework will pass this through to the SDK.
                options.RawRepresentationFactory = _ => new ChatCompletionOptions()
                {
#pragma warning disable OPENAI001
                    // Map the requested reasoning effort string directly. Acceptable inputs documented above.
                    // The pragma suppresses SDK analyzers that might flag setting provider specific fields.
                    ReasoningEffortLevel = reasoningEffort,
#pragma warning restore OPENAI001
                };
            }

            // Assign instructions to the ChatOptions. Instructions act as the agent's persistent system message.
            // They will be used by the agent whenever it composes messages to the model.
            options.Instructions = instructions;

            // If tools were provided, attach them to the ChatOptions.
            // The agent framework expects tools to be present on ChatOptions so it can surface them to the agent runtime.
            if (tools?.Count > 0)
            {
                options.Tools = tools;
            }

            // Convert the SDK ChatClient into the framework abstraction IChatClient.
            // This step isolates the rest of the agent code from the concrete SDK client.
            IChatClient chatClient = client.AsIChatClient();

            // If a clientFactory was provided allow the caller to wrap or replace the resolved IChatClient.
            // This is the extension point for adding telemetry, testing mocks, or request middleware.
            if (clientFactory is not null)
            {
                chatClient = clientFactory(chatClient);
            }

            // Build the ChatClientAgentOptions object that holds metadata and the chat options.
            ChatClientAgentOptions clientAgentOptions = new()
            {
                Name = name,
                Description = description,
                Instructions = instructions,
                ChatOptions = options
            };

            // Finally create and return the ChatClientAgent instance.
            // The agent will use the provided IChatClient for all model calls,
            // the provided loggerFactory for logging, and the optional services provider for resolving function dependencies.
            return new ChatClientAgent(chatClient, clientAgentOptions, loggerFactory, services);
        }
    }
}
