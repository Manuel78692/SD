namespace ANALISERPC.Models
{
    /// <summary>
    /// Request model for analysis operations
    /// </summary>
    public class AnaliseRequest
    {
        public string TipoSensor { get; set; } = string.Empty;
        public string TipoAnalise { get; set; } = string.Empty; // "basica", "tendencia", "anomalia", etc.
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<SensorDataPoint> Dados { get; set; } = new();
    }

    /// <summary>
    /// Response model for analysis results
    /// </summary>
    public class AnaliseResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public AnaliseBasica? EstatisticasBasicas { get; set; }
        public AnaliseTendencia? Tendencia { get; set; }
        public AnaliseAnomalia? Anomalias { get; set; }
    }

    /// <summary>
    /// Basic statistical analysis results
    /// </summary>
    public class AnaliseBasica
    {
        public double Media { get; set; }
        public double Mediana { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double DesvioPadrao { get; set; }
        public int TotalLeituras { get; set; }
    }

    /// <summary>
    /// Trend analysis results
    /// </summary>
    public class AnaliseTendencia
    {
        public string Direcao { get; set; } = string.Empty; // "crescente", "decrescente", "estavel"
        public double Inclinacao { get; set; }
        public double CorrelacaoTemporal { get; set; }
    }

    /// <summary>
    /// Anomaly detection results
    /// </summary>
    public class AnaliseAnomalia
    {
        public List<SensorDataPoint> LeiturasSuspeitas { get; set; } = new();
        public double LimiteInferior { get; set; }
        public double LimiteSuperior { get; set; }
        public int TotalAnomalias { get; set; }
    }

    /// <summary>
    /// Individual sensor data point for analysis
    /// </summary>
    public class SensorDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string WavyId { get; set; } = string.Empty;
        
        // For GPS readings
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // For Gyro readings
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }
    }
}
