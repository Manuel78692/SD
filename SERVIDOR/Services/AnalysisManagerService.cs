using Microsoft.EntityFrameworkCore;
using SERVIDOR.Data;
using SERVIDOR.Models;
using ANALISERPC.Models;

namespace SERVIDOR.Services
{
    /// <summary>
    /// Service that handles analysis requests by querying database and calling ANALISERPC
    /// </summary>
    public class AnalysisManagerService
    {
        private readonly SensorDataContext _context;

        public AnalysisManagerService(SensorDataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Performs analysis by querying database and calling ANALISERPC service
        /// </summary>
        public async Task<AnaliseResponse?> RealizarAnaliseAsync(string tipoSensor, string tipoAnalise, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                Console.WriteLine($"[.] Consultando dados: {tipoSensor} de {dataInicio:yyyy-MM-dd} a {dataFim:yyyy-MM-dd}");
                
                // Query database for sensor data
                var dadosSensor = await ObterDadosSensorAsync(tipoSensor, dataInicio, dataFim);
                
                if (dadosSensor.Count == 0)
                {
                    return new AnaliseResponse
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum dado encontrado para o período especificado"
                    };
                }

                Console.WriteLine($"[.] Encontrados {dadosSensor.Count} registros. Enviando para análise...");

                // Create analysis request
                var request = new AnaliseRequest
                {
                    TipoSensor = tipoSensor,
                    TipoAnalise = tipoAnalise,
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    Dados = dadosSensor
                };

                // Call ANALISERPC service
                var resultado = await AnaliseRPCClient.SolicitarAnaliseAsync(request);
                
                if (resultado == null)
                {
                    return new AnaliseResponse
                    {
                        Sucesso = false,
                        Mensagem = "Erro na comunicação com o servidor de análise"
                    };
                }

                Console.WriteLine($"[.] Análise concluída: {resultado.Mensagem}");
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Erro durante análise: {ex.Message}");
                return new AnaliseResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro interno: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Queries database for sensor data and converts to analysis format
        /// </summary>
        private async Task<List<SensorDataPoint>> ObterDadosSensorAsync(string tipoSensor, DateTime dataInicio, DateTime dataFim)
        {
            var dadosSensor = new List<SensorDataPoint>();

            // Normalize to lower case to ensure consistent matching
            switch (tipoSensor.ToLower())
            {
                case "temperatura": // Matches SensorDataService
                    var tempReadings = await _context.TemperatureReadings
                        .Where(r => r.Timestamp >= dataInicio && r.Timestamp <= dataFim)
                        .OrderBy(r => r.Timestamp)
                        .ToListAsync();
                    
                    dadosSensor = tempReadings.Select(r => new SensorDataPoint
                    {
                        Timestamp = r.Timestamp,
                        Value = r.Value,
                        WavyId = r.WavyId
                    }).ToList();
                    break;

                case "ph": // Matches SensorDataService
                    var phReadings = await _context.PhReadings
                        .Where(r => r.Timestamp >= dataInicio && r.Timestamp <= dataFim)
                        .OrderBy(r => r.Timestamp)
                        .ToListAsync();
                    
                    dadosSensor = phReadings.Select(r => new SensorDataPoint
                    {
                        Timestamp = r.Timestamp,
                        Value = r.Value,
                        WavyId = r.WavyId
                    }).ToList();
                    break;

                case "humidade": // Changed from "humidity" to "humidade" to match SensorDataService and MainUI
                    var humidityReadings = await _context.HumidityReadings
                        .Where(r => r.Timestamp >= dataInicio && r.Timestamp <= dataFim)
                        .OrderBy(r => r.Timestamp)
                        .ToListAsync();
                    
                    dadosSensor = humidityReadings.Select(r => new SensorDataPoint
                    {
                        Timestamp = r.Timestamp,
                        Value = r.Value,
                        WavyId = r.WavyId
                    }).ToList();
                    break;

                case "gps": // Matches SensorDataService
                    var gpsReadings = await _context.GpsReadings
                        .Where(r => r.Timestamp >= dataInicio && r.Timestamp <= dataFim)
                        .OrderBy(r => r.Timestamp)
                        .ToListAsync();
                    
                    dadosSensor = gpsReadings.Select(r => new SensorDataPoint
                    {
                        Timestamp = r.Timestamp,
                        Value = 0, // Not used for GPS
                        WavyId = r.WavyId,
                        Latitude = r.Latitude,
                        Longitude = r.Longitude
                    }).ToList();
                    break;

                case "gyro":
                    var gyroReadings = await _context.GyroReadings
                        .Where(r => r.Timestamp >= dataInicio && r.Timestamp <= dataFim)
                        .OrderBy(r => r.Timestamp)
                        .ToListAsync();
                    
                    dadosSensor = gyroReadings.Select(r => new SensorDataPoint
                    {
                        Timestamp = r.Timestamp,
                        Value = 0, // Not used for Gyro
                        WavyId = r.WavyId,
                        X = r.X,
                        Y = r.Y,
                        Z = r.Z
                    }).ToList();
                    break;

                default:
                    Console.WriteLine($"[!] Tipo de sensor não reconhecido: {tipoSensor}");
                    break;
            }

            return dadosSensor;
        }
    }
}
