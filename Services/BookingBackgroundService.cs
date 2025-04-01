using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Services
{
    public class BookingBackgroundService : BackgroundService
    {
        private readonly ILogger<BookingBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _regularCheckInterval = TimeSpan.FromSeconds(20);
        private readonly TimeSpan _criticalCheckInterval = TimeSpan.FromSeconds(2);
        private DateTime? _nextBookingStartTime;

        public BookingBackgroundService(
            ILogger<BookingBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Checking for upcoming bookings");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // Update next booking time if needed
                        await UpdateNextBookingTime(scope);
                        
                        // Check and notify clients
                        var bookingController = scope.ServiceProvider.GetRequiredService<BookingController>();
                        await bookingController.CheckAndNotifyUpcomingBookings();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for upcoming bookings.");
                }

                // Calculate delay until next check
                var delay = CalculateNextCheckDelay();
                
                _logger.LogDebug($"Next check in {delay.TotalSeconds} seconds");
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("Booking Background Service is stopping.");
        }
        
        private async Task UpdateNextBookingTime(IServiceScope scope)
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.Now;
                
                // Get the next upcoming booking start time
                var nextBooking = await dbContext.Bookings
                    .Where(b => b.StartTime > now && b.Status == BookingStatus.Reserved)
                    .OrderBy(b => b.StartTime)
                    .FirstOrDefaultAsync();
                
                _nextBookingStartTime = nextBooking?.StartTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next booking time");
            }
        }
        
        private TimeSpan CalculateNextCheckDelay()
        {
            if (!_nextBookingStartTime.HasValue)
            {
                return _regularCheckInterval;
            }
            
            var now = DateTime.Now;
            var timeUntilNextBooking = _nextBookingStartTime.Value - now;
            
            // If the next booking is within 1 minute, check very frequently
            if (timeUntilNextBooking.TotalMinutes <= 1)
            {
                return _criticalCheckInterval;
            }
            
            // If the next booking is within 2 minutes, check more frequently
            if (timeUntilNextBooking.TotalMinutes <= 2)
            {
                return TimeSpan.FromSeconds(5);
            }
            
            // If the next booking is within 10 minutes, check more frequently based on proximity
            if (timeUntilNextBooking.TotalMinutes <= 10)
            {
                // Gradually increase check frequency as we get closer to booking time
                // From 20 seconds (when 10 minutes away) down to 5 seconds (when 2 minutes away)
                var adjustedInterval = TimeSpan.FromSeconds(
                    (timeUntilNextBooking.TotalMinutes - 2) * (15.0 / 8.0) + 5
                );
                
                return adjustedInterval;
            }
            
            return _regularCheckInterval;
        }
    }
} 