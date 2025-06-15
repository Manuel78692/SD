/// <summary>
/// This is used for all RabbitMQ communications.
/// Key Concepts:
/// - Topic Exchange - Routes messages based on routing patterns
/// - Dead Letter Exchange (DLX) - Handles failed/undelivered messages
/// - TTL (Time To Live) - Messages expire after 5 seconds if not processed
/// - Fallback Queue - Safety net for failed message processing
/// </summary>
public static class RabbitMqConstants
{
    public const string HostName = "localhost";
    public const string WavyTopicExchange = "wavy_topic_router";
    public const string AgregadorFallebackDlx = "agregador_fallback_dlx";
    public const string AgregadorGeneralFallbackQueue = "agregador_general_fallback_q";
    // Used if DLX (Dead Letter eXchange) is direct/topic and needs a routing key to the fallback queue
    public const string FallbackRoutingKey = "fallback";
    public const int MessageTtlMilliseconds = 5000; // 5 seconds for a message to live in a preferred queue
}