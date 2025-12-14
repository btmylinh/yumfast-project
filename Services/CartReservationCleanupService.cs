namespace WebApp.Services
{
    /// <summary>
    /// Background service để release expired cart reservations
    /// Chạy mỗi 5 phút
    /// </summary>
    public class CartReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CartReservationCleanupService> _logger;

        public CartReservationCleanupService(
            IServiceProvider services,
            ILogger<CartReservationCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cart Reservation Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                    using var scope = _services.CreateScope();
                    var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

                    var releasedCount = await inventoryService.ReleaseExpiredReservationsAsync(15);
                    
                    if (releasedCount > 0)
                    {
                        _logger.LogInformation($"Released {releasedCount} expired cart reservations.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Cart Reservation Cleanup Service.");
                }
            }

            _logger.LogInformation("Cart Reservation Cleanup Service stopped.");
        }
    }
}
