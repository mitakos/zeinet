namespace ZEIage.Models.Infobip;

public enum InfobipCallState
{
    // Call setup states
    CALL_RECEIVED,
    CALL_RINGING,
    CALL_PRE_ESTABLISHED,
    CALL_ESTABLISHED,
    
    // Media states
    CALL_MEDIA_CHANGED,
    CALL_MEDIA_STREAM_CONNECTED,
    CALL_MEDIA_STREAM_DISCONNECTED,
    
    // End states
    CALL_FINISHED,
    CALL_FAILED,
    CALL_NO_ANSWER,
    CALL_BUSY,
    CALL_REJECTED,
    CALL_CANCELED,
    
    // Error states
    CALL_NETWORK_ERROR,
    CALL_TIMEOUT
} 