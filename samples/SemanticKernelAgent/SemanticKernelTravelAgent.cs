using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Polly;
using SharpA2A.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticKernelAgent;

#region Plugin
/// <summary>
/// A simple currency plugin that leverages Frankfurter for exchange rates.
/// The Plugin is used by the currency_exchange_agent.
/// </summary>
public class CurrencyPlugin
{
    private readonly ILogger<CurrencyPlugin> _logger;
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    /// <summary>
    /// Initialize a new instance of the CurrencyPlugin
    /// </summary>
    /// <param name="logger">Logger for the plugin</param>
    /// <param name="httpClientFactory">HTTP client factory for making API requests</param>
    public CurrencyPlugin(ILogger<CurrencyPlugin> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Create a retry policy for transient HTTP errors
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && IsTransientError(r))
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Retrieves exchange rate between currency_from and currency_to using Frankfurter API
    /// </summary>
    /// <param name="currencyFrom">Currency code to convert from, e.g. USD</param>
    /// <param name="currencyTo">Currency code to convert to, e.g. EUR or INR</param>
    /// <param name="date">Date or 'latest'</param>
    /// <returns>String representation of exchange rate</returns>
    [KernelFunction]
    [Description("Retrieves exchange rate between currency_from and currency_to using Frankfurter API")]
    public async Task<string> GetExchangeRateAsync(
        [Description("Currency code to convert from, e.g. USD")] string currencyFrom,
        [Description("Currency code to convert to, e.g. EUR or INR")] string currencyTo,
        [Description("Date or 'latest'")] string date = "latest")
    {
        try
        {
            _logger.LogInformation("Getting exchange rate from {CurrencyFrom} to {CurrencyTo} for date {Date}",
                currencyFrom, currencyTo, date);

            // Build request URL with query parameters
            var requestUri = $"https://api.frankfurter.app/{date}?from={Uri.EscapeDataString(currencyFrom)}&to={Uri.EscapeDataString(currencyTo)}";

            // Use retry policy for resilience
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(requestUri));
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            if (!data.TryGetProperty("rates", out var rates) ||
                !rates.TryGetProperty(currencyTo, out var rate))
            {
                _logger.LogWarning("Could not retrieve rate for {CurrencyFrom} to {CurrencyTo}", currencyFrom, currencyTo);
                return $"Could not retrieve rate for {currencyFrom} to {currencyTo}";
            }

            return $"1 {currencyFrom} = {rate.GetDecimal()} {currencyTo}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate from {CurrencyFrom} to {CurrencyTo}", currencyFrom, currencyTo);
            return $"Currency API call failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Checks if the HTTP response indicates a transient error
    /// </summary>
    /// <param name="response">HTTP response message</param>
    /// <returns>True if the status code indicates a transient error</returns>
    private bool IsTransientError(HttpResponseMessage response)
    {
        int statusCode = (int)response.StatusCode;
        return statusCode == 408 // Request Timeout
            || statusCode == 429 // Too Many Requests 
            || statusCode >= 500 && statusCode < 600; // Server errors
    }
}
#endregion

#region Response Format
/// <summary>
/// A Response Format model to direct how the model should respond
/// </summary>
public class ResponseFormat
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "input_required";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    public static ResponseFormat FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ResponseFormat>(json) ??
                   new ResponseFormat { Status = "error", Message = "Failed to parse response" };
        }
        catch
        {
            return new ResponseFormat { Status = "error", Message = "Invalid response format" };
        }
    }
}
#endregion

#region Semantic Kernel Agent

/// <summary>
/// Wraps Semantic Kernel-based agents to handle Travel related tasks
/// </summary>
public class SemanticKernelTravelAgent : IDisposable
{
    public static readonly ActivitySource ActivitySource = new ActivitySource("A2A.SemanticKernelTravelAgent", "1.0.0");

    /// <summary>
    /// Initializes a new instance of the SemanticKernelTravelAgent
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger for the agent</param>
    /// <param name="httpClient">HTTP client</param>
    public SemanticKernelTravelAgent(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Create currency plugin
        _currencyPlugin = new CurrencyPlugin(
            logger: new Logger<CurrencyPlugin>(new LoggerFactory()),
            httpClient: _httpClient);

        // Initialize the agent
        _agent = InitializeAgent();
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Attach(TaskManager taskManager)
    {
        this._taskManager = taskManager;
        taskManager.OnTaskCreated = ExecuteAgentTask;
        taskManager.OnTaskUpdated = ExecuteAgentTask;
        taskManager.OnAgentCardQuery = GetAgentCard;
    }

    public async Task ExecuteAgentTask(AgentTask task)
    {
        if (_taskManager == null)
        {
            throw new Exception("TaskManager is not attached.");
        }

        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working);

        // Get message from the user
        var userMessage = task.History!.Last().Parts.First().AsTextPart().Text;

        // Get the response from the agent
        var artifact = new Artifact();
        await foreach (AgentResponseItem<ChatMessageContent> response in _agent.InvokeAsync(userMessage))
        {
            var content = response.Message.Content;
            artifact.Parts.Add(new TextPart() { Text = content! });
        }

        // Return as artifacts
        await _taskManager.ReturnArtifactAsync(task.Id, artifact);
        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Completed);
    }

    public AgentCard GetAgentCard(string agentUrl)
    {
        var capabilities = new AgentCapabilities()
        {
            Streaming = false,
            PushNotifications = false,
        };

        var skillTripPlanning = new AgentSkill()
        {
            Id = "trip_planning_sk",
            Name = "Semantic Kernel Trip Planning",
            Description = "Handles comprehensive trip planning, including currency exchanges, itinerary creation, sightseeing, dining recommendations, and event bookings using Frankfurter API for currency conversions.",
            Tags = ["trip", "planning", "travel", "currency", "semantic-kernel"],
            Examples =
            [
                "I am from Korea. Plan a budget-friendly day trip to Dublin including currency exchange.",
                "I am from Korea. What's the exchange rate and recommended itinerary for visiting Galway?",
            ],
        };

        return new AgentCard()
        {
            Name = "SK Travel Agent",
            Description = "Semantic Kernel-based travel agent providing comprehensive trip planning services including currency exchange and personalized activity planning.",
            Url = agentUrl,
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [skillTripPlanning],
        };
    }

    /// <summary>
    /// Handle synchronous tasks (like tasks/send)
    /// </summary>
    /// <param name="userInput">User input message</param>
    /// <param name="sessionId">Unique identifier for the session</param>
    /// <returns>A dictionary containing the content, task completion status, and user input requirement</returns>
    public async Task<Dictionary<string, object>> InvokeAsync(string userInput, string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing user request for session {SessionId}", sessionId);
            await EnsureThreadExistsAsync(sessionId);

            var response = _agent.InvokeAsync(
                message: userInput,
                thread: _thread,
                cancellationToken: cancellationToken);

            return await GetAgentResponseAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for session {SessionId}", sessionId);
            return new Dictionary<string, object>
            {
                ["is_task_complete"] = false,
                ["require_user_input"] = true,
                ["content"] = $"An error occurred while processing your request: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// For streaming tasks (like tasks/sendSubscribe), we yield partial progress using SK agent's invoke_stream
    /// </summary>
    /// <param name="userInput">User input message</param>
    /// <param name="sessionId">Unique identifier for the session</param>
    /// <returns>An async enumerable of dictionaries containing the content, task completion status, and user input requirement</returns>
    public async IAsyncEnumerable<Dictionary<string, object>> InvokeStreamingAsync(string userInput, string sessionId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting streaming request for session {SessionId}", sessionId);
        await EnsureThreadExistsAsync(sessionId);

        List<StreamingChatMessageContent> chunks = new();

        // For the sample, to avoid too many messages, only show one "in-progress" message for each task
        bool toolCallInProgress = false;
        bool messageInProgress = false;

        await foreach (var streamingContent in _agent.InvokeStreamingAsync(
            message: userInput,
            thread: _thread)
            .WithCancellation(cancellationToken))
        {
            // Check if we have a function call in progress
            bool hasFunctionCall = streamingContent.Message.Items.Any(item => item is StreamingFunctionCallUpdateContent);

            if (hasFunctionCall && !toolCallInProgress)
            {
                toolCallInProgress = true;
                yield return new Dictionary<string, object>
                {
                    ["is_task_complete"] = false,
                    ["require_user_input"] = false,
                    ["content"] = "Processing the trip plan (with plugins)..."
                };
            }
            else if (!hasFunctionCall && !messageInProgress && streamingContent.Message.Items.Any(item => item is StreamingTextContent))
            {
                messageInProgress = true;
                yield return new Dictionary<string, object>
                {
                    ["is_task_complete"] = false,
                    ["require_user_input"] = false,
                    ["content"] = "Building the trip plan..."
                };
            }

            // Collect all chunks to build the final message
            chunks.Add(streamingContent);
        }

        // Once streaming is done, combine all chunks and return final result
        if (chunks.Count > 0)
        {
            // Extract the content from all chunks
            var content = string.Join("", chunks.Select(c =>
                string.Join("", c.Items
                    .Where(i => i is StreamingTextContent)
                    .Select(i => ((StreamingTextContent)i).Text))));

            yield return GetAgentResponse(content);
        }
        else
        {
            yield return new Dictionary<string, object>
            {
                ["is_task_complete"] = false,
                ["require_user_input"] = true,
                ["content"] = "No response was generated. Please try again."
            };
        }
    }



    #region private
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly CurrencyPlugin _currencyPlugin;
    private readonly HttpClient _httpClient;
    private readonly ChatCompletionAgent _agent;
    private TaskManager? _taskManager;

    private ChatHistoryAgentThread? _thread;

    public readonly List<string> SupportedContentTypes = new() { "text", "text/plain" };

    private ChatCompletionAgent InitializeAgent()
    {
        try
        {
            string apiKey = _configuration["OPENAI_API_KEY"] ?? throw new ArgumentException("OPENAI_API_KEY must be provided");
            string modelId = _configuration["OPENAI_CHAT_MODEL_ID"] ?? "gpt-4.1";

            _logger.LogInformation("Initializing Semantic Kernel agent with model {ModelId}", modelId);

            // Define the TravelPlannerAgent
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(modelId, apiKey);
            builder.Plugins.AddFromObject(_currencyPlugin);
            var kernel = builder.Build();
            var travelPlannerAgent = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "TravelPlannerAgent",
                Instructions =
                    """
                    You specialize in planning and recommending activities for travelers.
                    This includes suggesting sightseeing options, local events, dining recommendations,
                    booking tickets for attractions, advising on travel itineraries, and ensuring activities
                    align with traveler preferences and schedule.
                    Your goal is to create enjoyable and personalized experiences for travelers.
                    You specialize in planning and recommending activities for travelers.
                    This includes suggesting sightseeing options, local events, dining recommendations,
                    booking tickets for attractions, advising on travel itineraries, and ensuring activities
                    align with traveler preferences and schedule.
                    This includes providing current exchange rates, converting amounts between different currencies,
                    explaining fees or charges related to currency exchange, and giving advice on the best practices for exchanging currency.
                    Your goal is to create enjoyable and personalized experiences for travelers.
                    """
            };

            return travelPlannerAgent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Semantic Kernel agent");
            throw;
        }
    }

    /// <summary>
    /// Ensure the thread exists for the given session ID
    /// </summary>
    /// <param name="sessionId">Unique identifier for the session</param>
    private async Task EnsureThreadExistsAsync(string sessionId)
    {
        if (_thread == null || _thread.Id != sessionId)
        {
            if (_thread != null)
            {
                await Task.CompletedTask; // In C# we don't need to explicitly delete the thread
            }

            _thread = new ChatHistoryAgentThread([], sessionId);
            _logger.LogInformation("Created new thread for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Extracts the structured response from the agent's message content
    /// </summary>
    /// <param name="content">The message content from the agent</param>
    /// <returns>A dictionary containing the content, task completion status, and user input requirement</returns>
    private async Task<Dictionary<string, object>> GetAgentResponseAsync(IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> responses)
    {
        StringBuilder content = new();
        await foreach (AgentResponseItem<ChatMessageContent> response in responses)
        {
            content.Append(response.Message.Content);
        }

        return GetAgentResponse(content.ToString());
    }

    /// <summary>
    /// Extracts the structured response from the agent's message content
    /// </summary>
    /// <param name="content">The message content from the agent</param>
    /// <returns>A dictionary containing the content, task completion status, and user input requirement</returns>
    private async Task<Dictionary<string, object>> GetAgentResponseAsync(IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> responses)
    {
        StringBuilder content = new();
        await foreach (AgentResponseItem<StreamingChatMessageContent> response in responses)
        {
            content.Append(response.Message.Content);
        }

        return GetAgentResponse(content.ToString());
    }

    /// <summary>
    /// Extracts the structured response from the agent's message content
    /// </summary>
    /// <param name="content">The message content from the agent</param>
    /// <returns>A dictionary containing the content, task completion status, and user input requirement</returns>
    private Dictionary<string, object> GetAgentResponse(string content)
    {
        try
        {
            var structuredResponse = ResponseFormat.FromJson(content);

            var defaultResponse = new Dictionary<string, object>
            {
                ["is_task_complete"] = false,
                ["require_user_input"] = true,
                ["content"] = "We are unable to process your request at the moment. Please try again."
            };

            var responseMap = new Dictionary<string, Dictionary<string, object>>
            {
                ["input_required"] = new()
                {
                    ["is_task_complete"] = false,
                    ["require_user_input"] = true
                },
                ["error"] = new()
                {
                    ["is_task_complete"] = false,
                    ["require_user_input"] = true
                },
                ["completed"] = new()
                {
                    ["is_task_complete"] = true,
                    ["require_user_input"] = false
                }
            };

            if (responseMap.TryGetValue(structuredResponse.Status, out var responseDict))
            {
                responseDict["content"] = structuredResponse.Message;
                return responseDict;
            }

            return defaultResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing agent response");
            return new Dictionary<string, object>
            {
                ["is_task_complete"] = false,
                ["require_user_input"] = true,
                ["content"] = "We encountered an error parsing the response. Please try again."
            };
        }
    }

    #endregion
}
#endregion

