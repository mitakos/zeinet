namespace ZEIage.Models
{
    /// <summary>
    /// Defines all possible states a call can be in
    /// </summary>
    public static class CallSessionState
    {
        // Initial states
        public const string Initializing = "INITIALIZING";
        public const string ElevenLabsReady = "ELEVENLABS_READY";
        public const string CallInitiated = "CALL_INITIATED";
        
        // Call progress states
        public const string Calling = "CALLING";
        public const string Ringing = "CALL_RINGING";
        public const string PreEstablished = "CALL_PRE_ESTABLISHED";
        public const string Established = "ESTABLISHED";
        public const string MediaChanged = "CALL_MEDIA_CHANGED";
        
        // Terminal states
        public const string Rejected = "CALL_REJECTED";
        public const string Busy = "CALL_BUSY";
        public const string NoAnswer = "CALL_NO_ANSWER";
        public const string Failed = "FAILED";
        public const string Finished = "FINISHED";
        
        // Recording states
        public const string Recording = "CALL_RECORDING_STARTED";
        public const string RecordingFailed = "CALL_RECORDING_FAILED";
        public const string RecordingStopped = "CALL_RECORDING_STOPPED";
    }
} 