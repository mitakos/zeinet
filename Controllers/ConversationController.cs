using Microsoft.AspNetCore.Mvc;
using ZEIage.Models.ElevenLabs;
using ZEIage.Services;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Handles endpoints for retrieving conversation data from ElevenLabs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly ILogger<ConversationController> _logger;
        private readonly ElevenLabsService _elevenLabsService;

        public ConversationController(
            ILogger<ConversationController> logger,
            ElevenLabsService elevenLabsService)
        {
            _logger = logger;
            _elevenLabsService = elevenLabsService;
        }

        /// <summary>
        /// Gets all conversations for the agent
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ElevenLabsConversation>>> GetConversations(
            [FromQuery] int pageSize = 30)
        {
            try
            {
                var conversations = await _elevenLabsService.GetConversationsAsync(pageSize: pageSize);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations");
                return StatusCode(500, "Error retrieving conversations");
            }
        }

        /// <summary>
        /// Gets detailed data for a specific conversation
        /// </summary>
        [HttpGet("{conversationId}")]
        public async Task<ActionResult<ElevenLabsConversation>> GetConversationDetails(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound($"Conversation {conversationId} not found");
                }
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation {ConversationId}", conversationId);
                return StatusCode(500, "Error retrieving conversation details");
            }
        }

        /// <summary>
        /// Gets the transcript for a specific conversation
        /// </summary>
        [HttpGet("{conversationId}/transcript")]
        public async Task<ActionResult<List<ElevenLabsMessage>>> GetConversationTranscript(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound($"Conversation {conversationId} not found");
                }
                return Ok(conversation.Messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transcript for conversation {ConversationId}", conversationId);
                return StatusCode(500, "Error retrieving conversation transcript");
            }
        }

        /// <summary>
        /// Gets collected variables from a conversation
        /// </summary>
        [HttpGet("{conversationId}/variables")]
        public async Task<ActionResult<Dictionary<string, string>>> GetCollectedVariables(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound($"Conversation {conversationId} not found");
                }

                var variables = conversation.Messages
                    .Where(m => m.CollectedVariables.Count > 0)
                    .SelectMany(m => m.CollectedVariables)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                return Ok(variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving variables for conversation {ConversationId}", conversationId);
                return StatusCode(500, "Error retrieving conversation variables");
            }
        }

        /// <summary>
        /// Gets analysis data for a conversation
        /// </summary>
        [HttpGet("{conversationId}/analysis")]
        public async Task<ActionResult<ElevenLabsAnalysis>> GetConversationAnalysis(string conversationId)
        {
            try
            {
                var conversation = await _elevenLabsService.GetConversationDetailsAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound($"Conversation {conversationId} not found");
                }
                return Ok(conversation.Analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis for conversation {ConversationId}", conversationId);
                return StatusCode(500, "Error retrieving conversation analysis");
            }
        }
    }
} 