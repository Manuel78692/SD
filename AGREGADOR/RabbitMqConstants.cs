// RabbitMqConstants.cs (or define within relevant classes)
public static class RabbitMqConstants
{
    public const string HostName = "localhost"; // Or your RabbitMQ server address
    public const string WavyTopicExchange = "wavy_topic_router";
    public const string AgregadorFallebackDlx = "agregador_fallback_dlx";
    public const string AgregadorGeneralFallbackQueue = "agregador_general_fallback_q";
    // Used if DLX is direct/topic and needs a routing key to the fallback queue
    public const string FallbackRoutingKey = "fallback"; 
    public const int MessageTtlMilliseconds = 5000; // 5 seconds for a message to live in a preferred queue
}