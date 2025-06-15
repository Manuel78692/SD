using ANALISERPC.Models;

namespace ANALISERPC.Services
{
    /// <summary>
    /// Service responsible for performing various types of sensor data analysis
    /// </summary>
    public class AnalysisService
    {
        /// <summary>
        /// Performs analysis based on the request type
        /// </summary>
        public static AnaliseResponse ProcessarAnalise(AnaliseRequest request)
        {
            var response = new AnaliseResponse { Sucesso = true };

            try
            {
                if (request.Dados == null || request.Dados.Count == 0)
                {
                    response.Sucesso = false;
                    response.Mensagem = "Nenhum dado fornecido para análise";
                    return response;
                }

                switch (request.TipoAnalise.ToLower())
                {
                    case "basica":
                        response.EstatisticasBasicas = CalcularEstatisticasBasicas(request.Dados, request.TipoSensor);
                        response.Mensagem = "Análise básica concluída com sucesso";
                        break;
                    
                    case "tendencia":
                        response.Tendencia = CalcularTendencia(request.Dados, request.TipoSensor);
                        response.Mensagem = "Análise de tendência concluída com sucesso";
                        break;
                    
                    case "anomalia":
                        response.Anomalias = DetectarAnomalias(request.Dados, request.TipoSensor);
                        response.Mensagem = "Detecção de anomalias concluída com sucesso";
                        break;
                    
                    case "completa":
                        response.EstatisticasBasicas = CalcularEstatisticasBasicas(request.Dados, request.TipoSensor);
                        response.Tendencia = CalcularTendencia(request.Dados, request.TipoSensor);
                        response.Anomalias = DetectarAnomalias(request.Dados, request.TipoSensor);
                        response.Mensagem = "Análise completa concluída com sucesso";
                        break;
                    
                    default:
                        response.Sucesso = false;
                        response.Mensagem = $"Tipo de análise não reconhecido: {request.TipoAnalise}";
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Sucesso = false;
                response.Mensagem = $"Erro durante análise: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Calculates basic statistical measures
        /// </summary>
        private static AnaliseBasica CalcularEstatisticasBasicas(List<SensorDataPoint> dados, string tipoSensor)
        {
            var valores = ExtrairValores(dados, tipoSensor);
            
            if (valores.Count == 0)
                return new AnaliseBasica();

            var ordenados = valores.OrderBy(v => v).ToList();
            var media = valores.Average();
            var mediana = ordenados.Count % 2 == 0 
                ? (ordenados[ordenados.Count / 2 - 1] + ordenados[ordenados.Count / 2]) / 2.0
                : ordenados[ordenados.Count / 2];
            
            var variancia = valores.Sum(v => Math.Pow(v - media, 2)) / valores.Count;
            var desvioPadrao = Math.Sqrt(variancia);

            return new AnaliseBasica
            {
                Media = Math.Round(media, 3),
                Mediana = Math.Round(mediana, 3),
                Max = valores.Max(),
                Min = valores.Min(),
                DesvioPadrao = Math.Round(desvioPadrao, 3),
                TotalLeituras = valores.Count
            };
        }

        /// <summary>
        /// Calculates trend analysis using linear regression
        /// </summary>
        private static AnaliseTendencia CalcularTendencia(List<SensorDataPoint> dados, string tipoSensor)
        {
            var valores = ExtrairValores(dados, tipoSensor);
            
            if (valores.Count < 2)
                return new AnaliseTendencia { Direcao = "indeterminado" };

            // Simple linear regression for trend
            var n = valores.Count;
            var sumX = 0.0;
            var sumY = valores.Sum();
            var sumXY = 0.0;
            var sumX2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumXY += i * valores[i];
                sumX2 += i * i;
            }

            var inclinacao = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var correlacao = CalcularCorrelacao(valores);

            var direcao = Math.Abs(inclinacao) < 0.001 ? "estavel" 
                        : inclinacao > 0 ? "crescente" : "decrescente";

            return new AnaliseTendencia
            {
                Direcao = direcao,
                Inclinacao = Math.Round(inclinacao, 6),
                CorrelacaoTemporal = Math.Round(correlacao, 3)
            };
        }

        /// <summary>
        /// Detects anomalies using statistical methods (IQR method)
        /// </summary>
        private static AnaliseAnomalia DetectarAnomalias(List<SensorDataPoint> dados, string tipoSensor)
        {
            var valores = ExtrairValores(dados, tipoSensor);
            
            if (valores.Count < 4)
                return new AnaliseAnomalia();

            var ordenados = valores.OrderBy(v => v).ToList();
            var q1Index = (int)(ordenados.Count * 0.25);
            var q3Index = (int)(ordenados.Count * 0.75);
            var q1 = ordenados[q1Index];
            var q3 = ordenados[q3Index];
            var iqr = q3 - q1;
            
            var limiteInferior = q1 - (1.5 * iqr);
            var limiteSuperior = q3 + (1.5 * iqr);

            var anomalias = new List<SensorDataPoint>();
            for (int i = 0; i < dados.Count; i++)
            {
                var valor = ObterValor(dados[i], tipoSensor);
                if (valor < limiteInferior || valor > limiteSuperior)
                {
                    anomalias.Add(dados[i]);
                }
            }

            return new AnaliseAnomalia
            {
                LeiturasSuspeitas = anomalias,
                LimiteInferior = Math.Round(limiteInferior, 3),
                LimiteSuperior = Math.Round(limiteSuperior, 3),
                TotalAnomalias = anomalias.Count
            };
        }

        /// <summary>
        /// Extracts numeric values from sensor data points based on sensor type
        /// </summary>
        private static List<double> ExtrairValores(List<SensorDataPoint> dados, string tipoSensor)
        {
            return dados.Select(d => ObterValor(d, tipoSensor))
                       .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                       .ToList();
        }

        /// <summary>
        /// Gets the appropriate numeric value from a data point based on sensor type
        /// </summary>
        private static double ObterValor(SensorDataPoint dado, string tipoSensor)
        {
            return tipoSensor.ToLower() switch
            {
                "temperature" or "ph" or "humidity" => dado.Value,
                "gps" => Math.Sqrt(Math.Pow(dado.Latitude ?? 0, 2) + Math.Pow(dado.Longitude ?? 0, 2)), // Distance from origin
                "gyro" => Math.Sqrt(Math.Pow(dado.X ?? 0, 2) + Math.Pow(dado.Y ?? 0, 2) + Math.Pow(dado.Z ?? 0, 2)), // Magnitude
                _ => dado.Value
            };
        }

        /// <summary>
        /// Calculates temporal correlation coefficient
        /// </summary>
        private static double CalcularCorrelacao(List<double> valores)
        {
            if (valores.Count < 2) return 0;

            var n = valores.Count;
            var indices = Enumerable.Range(0, n).Select(i => (double)i).ToList();
            
            var mediaIndices = indices.Average();
            var mediaValores = valores.Average();
            
            var numerador = indices.Zip(valores, (x, y) => (x - mediaIndices) * (y - mediaValores)).Sum();
            var denominadorX = Math.Sqrt(indices.Sum(x => Math.Pow(x - mediaIndices, 2)));
            var denominadorY = Math.Sqrt(valores.Sum(y => Math.Pow(y - mediaValores, 2)));
            
            if (denominadorX == 0 || denominadorY == 0) return 0;
            
            return numerador / (denominadorX * denominadorY);
        }
    }
}
