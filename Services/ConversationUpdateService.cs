using Microsoft.Extensions.Hosting;
using ZEIage.Models;
using ZEIage.Models.ElevenLabs;

namespace ZEIage.Services
{
    /// <summary>
    /// Background service that periodically updates conversation data from ElevenLabs
    /// </summary>
    public class ConversationUpdateService : BackgroundService
    {
        private readonly ILogger<ConversationUpdateService> _logger;
        private readonly SessionManager _sessionManager;
        private readonly ElevenLabsService _elevenLabsService;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30);

        public ConversationUpdateService(
            ILogger<ConversationUpdateService> logger,
            SessionManager sessionManager,
            ElevenLabsService elevenLabsService)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _elevenLabsService = elevenLabsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateActiveSessions(stoppingToken);
                    await Task.Delay(_updateInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating conversations");
                }
            }
        }

        private async Task UpdateActiveSessions(CancellationToken ct)
        {
            var activeSessions = _sessionManager.GetAllSessions()
                .Where(s => s.State == CallSessionState.Established && !string.IsNullOrEmpty(s.ConversationId));

            foreach (var session in activeSessions)
            {
                try
                {
                    var conversation = await _elevenLabsService.GetConversationDetailsAsync(
                        session.ConversationId!, 
                        ct);

                    if (conversation == null)
                    {
                        _logger.LogWarning(
                            "No conversation details found for session {SessionId}, conversation {ConversationId}",
                            session.SessionId,
                            session.ConversationId);
                        continue;
                    }

                    // Update session with latest conversation data
                    _sessionManager.UpdateSession(session.SessionId, s =>
                    {
                        s.Transcript = conversation.Messages;
                        s.Variables = conversation.Variables;
                        s.Metadata = conversation.Metadata;
                        s.Analysis = conversation.Analysis;
                    });

                    _logger.LogDebug(
                        "Updated conversation data for session {SessionId}, conversation {ConversationId}",
                        session.SessionId,
                        session.ConversationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error updating conversation for session {SessionId}, conversation {ConversationId}",
                        session.SessionId,
                        session.ConversationId);
                }
            }
        }
    }
} 