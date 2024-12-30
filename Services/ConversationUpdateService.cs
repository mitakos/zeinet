using Microsoft.Extensions.Hosting;
using ZEIage.Models.ElevenLabs;

namespace ZEIage.Services
{
    /// <summary>
    /// Background service that periodically updates conversation data from ElevenLabs
    /// Keeps session data synchronized with the latest conversation state
    /// </summary>
    public class ConversationUpdateService : BackgroundService
    {
        private readonly ElevenLabsService _elevenLabsService;
        private readonly SessionManager _sessionManager;
        private readonly ILogger<ConversationUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the ConversationUpdateService
        /// </summary>
        /// <param name="elevenLabsService">Service for ElevenLabs API interactions</param>
        /// <param name="sessionManager">Service for managing call sessions</param>
        /// <param name="logger">Logger for tracking update operations</param>
        public ConversationUpdateService(
            ElevenLabsService elevenLabsService,
            SessionManager sessionManager,
            ILogger<ConversationUpdateService> logger)
        {
            _elevenLabsService = elevenLabsService;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <summary>
        /// Executes the background service
        /// Periodically fetches updated conversation data for active sessions
        /// </summary>
        /// <param name="stoppingToken">Token that signals when the service should stop</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeSessions = _sessionManager.GetActiveSessions();
                    foreach (var session in activeSessions)
                    {
                        if (!string.IsNullOrEmpty(session.ConversationId))
                        {
                            var conversation = await _elevenLabsService.GetConversationDetailsAsync(session.ConversationId);
                            UpdateSessionWithConversationData(session, conversation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating conversation data");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        /// <summary>
        /// Updates a session with the latest conversation data from ElevenLabs
        /// </summary>
        /// <param name="session">The session to update</param>
        /// <param name="conversation">New conversation data from ElevenLabs</param>
        private void UpdateSessionWithConversationData(CallSession session, ElevenLabsConversation conversation)
        {
            session.Transcript = conversation.Transcript;
            session.Metadata = conversation.Metadata;
            session.Analysis = conversation.Analysis;
            
            // Update collected variables
            foreach (var message in conversation.Transcript)
            {
                if (message.CollectedVariables != null)
                {
                    foreach (var variable in message.CollectedVariables)
                    {
                        session.Variables[variable.Key] = variable.Value;
                    }
                }
            }
        }
    }
} 