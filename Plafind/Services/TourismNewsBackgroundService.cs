using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Plafind.Services
{
    /// <summary>
    /// Turizm haberlerini periyodik olarak senkronize eden background service
    /// </summary>
    public class TourismNewsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TourismNewsBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(6); // Her 6 saatte bir çalışır
        
        // Background service durumu için static değişkenler
        private static bool _isRunning = false;
        private static DateTime _serviceStartTime = DateTime.MinValue;
        private static int _totalRuns = 0;
        private static DateTime _lastRunTime = DateTime.MinValue;

        public TourismNewsBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TourismNewsBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Background service durumunu döndürür
        /// </summary>
        public static (bool IsRunning, DateTime StartTime, int TotalRuns, DateTime LastRunTime) GetServiceStatus()
        {
            return (_isRunning, _serviceStartTime, _totalRuns, _lastRunTime);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _isRunning = true;
            _serviceStartTime = DateTime.Now;
            
            _logger.LogInformation("=== Turizm Haberleri Background Service Başlatıldı ===");
            _logger.LogInformation("Başlangıç Zamanı: {StartTime}", _serviceStartTime);
            _logger.LogInformation("Senkronizasyon Periyodu: {Period} saat", _period.TotalHours);
            _logger.LogInformation("===================================================");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _lastRunTime = DateTime.Now;
                    _totalRuns++;
                    
                    _logger.LogInformation("=== Otomatik Senkronizasyon #{RunNumber} Başlatıldı ===", _totalRuns);
                    _logger.LogInformation("Zaman: {Time}", _lastRunTime);
                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var newsService = scope.ServiceProvider.GetRequiredService<IGoogleNewsService>();
                        
                        await newsService.SyncTourismNewsToDatabaseAsync(maxItems: 20);
                        
                        _logger.LogInformation("=== Otomatik Senkronizasyon #{RunNumber} Tamamlandı ===", _totalRuns);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "=== Otomatik Senkronizasyon #{RunNumber} Hatası ===", _totalRuns);
                    _logger.LogError("Hata: {Message}", ex.Message);
                }

                // Bir sonraki çalışma zamanına kadar bekle
                var nextRunTime = DateTime.Now.Add(_period);
                _logger.LogInformation("Bir sonraki senkronizasyon: {NextRunTime}", nextRunTime);
                await Task.Delay(_period, stoppingToken);
            }

            _isRunning = false;
            _logger.LogInformation("=== Turizm Haberleri Background Service Durduruldu ===");
            _logger.LogInformation("Toplam çalıştırma sayısı: {TotalRuns}", _totalRuns);
        }
    }
}

