using Microsoft.AspNetCore.Mvc;
using ZEIage.Models.ElevenLabs;
using ZEIage.Services;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Controller for managing and retrieving conversation data from ElevenLabs
    /// Provides endpoints for accessing transcripts, variables, and analysis
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly ElevenLabsService _elevenLabsService;
        private readonly SessionManager _sessionManager;
        private readonly ILogger<ConversationController> _logger;

        /// <summary>
        /// Initializes the conversation controller with required services
        /// </summary>
        /// <param name="elevenLabsService">Service for ElevenLabs API interactions</param>
        /// <param name="sessionManager">Service for managing call sessions</param>
        /// <param name="logger">Logger for controller operations</param>
        public ConversationController(
            ElevenLabsService elevenLabsService,
            SessionManager sessionManager,
            ILogger<ConversationController> logger)
        {
            _elevenLabsService = elevenLabsService;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of all conversations for the agent
        /// </summary>
        /// <param name="pageSize">Number of conversations to retrieve per page (default: 30)</param>
        /// <returns>List of conversations or error details</returns>
        /// <response code="200">Returns the list of conversations</response>
        /// <response code="500">If there was an error retrieving the conversations</response>
        [HttpGet]
        public async Task<IActionResult> GetConversations([FromQuery] int pageSize = 30)
        {
            try
            {
                var conversations = await _elevenLabsService.GetConversationsAsync(pageSize: pageSize);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations");
                return StatusCode(500, new { error = "Failed to retrieve conversations", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific conversation
        /// </summary>
        /// <param name="conversationId">Unique identifier of the conversation</param>
        /// <returns>Conversation details or error message</returns>
        /// <response code="200">Returns the conversation details</response>
        /// <response code="404">If the conversation was not found</response>
        /// <response code="500">If there was an error retrieving the details</response>
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetConversationDetails(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation details for {ConversationId}", conversationId);
                return StatusCode(500, new { error = "Failed to retrieve conversation details", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the transcript of a specific conversation
        /// </summary>
        /// <param name="conversationId">Unique identifier of the conversation</param>
        /// <returns>Conversation transcript or error message</returns>
        /// <response code="200">Returns the conversation transcript</response>
        /// <response code="404">If the conversation was not found</response>
        /// <response code="500">If there was an error retrieving the transcript</response>
        [HttpGet("{conversationId}/transcript")]
        public async Task<IActionResult> GetConversationTranscript(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }
                return Ok(conversation.Transcript);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transcript for {ConversationId}", conversationId);
                return StatusCode(500, new { error = "Failed to retrieve transcript", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves variables collected during a specific conversation
        /// </summary>
        /// <param name="conversationId">Unique identifier of the conversation</param>
        /// <returns>Dictionary of collected variables or error message</returns>
        /// <response code="200">Returns the collected variables</response>
        /// <response code="404">If the conversation was not found</response>
        /// <response code="500">If there was an error retrieving the variables</response>
        [HttpGet("{conversationId}/variables")]
        public async Task<IActionResult> GetCollectedVariables(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }

                var variables = new Dictionary<string, string>();
                foreach (var message in conversation.Transcript)
                {
                    if (message.CollectedVariables != null)
                    {
                        foreach (var variable in message.CollectedVariables)
                        {
                            variables[variable.Key] = variable.Value;
                        }
                    }
                }

                return Ok(variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving variables for {ConversationId}", conversationId);
                return StatusCode(500, new { error = "Failed to retrieve variables", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves analysis data for a specific conversation
        /// Includes sentiment analysis and key topics discussed
        /// </summary>
        /// <param name="conversationId">Unique identifier of the conversation</param>
        /// <returns>Conversation analysis data or error message</returns>
        /// <response code="200">Returns the conversation analysis</response>
        /// <response code="404">If the conversation was not found</response>
        /// <response code="500">If there was an error retrieving the analysis</response>
        [HttpGet("{conversationId}/analysis")]
        public async Task<IActionResult> GetConversationAnalysis(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }
                return Ok(conversation.Analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis for {ConversationId}", conversationId);
                return StatusCode(500, new { error = "Failed to retrieve analysis", details = ex.Message });
            }
        }
    }
} 