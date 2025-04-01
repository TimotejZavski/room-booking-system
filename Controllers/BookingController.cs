using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Extensions;
using WebApplication1.Hubs;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<BookingHub> _hubContext;

    public BookingController(ApplicationDbContext context, IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Main booking page - public access
    public async Task<IActionResult> Index()
    {
        var currentBooking = await GetCurrentBookingAsync();
        
        // Get today's date and tomorrow's date without time component
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        
        // Group bookings by date - include all non-cancelled bookings
        var todayBookings = await _context.Bookings
            .Where(b => b.StartTime.Date == today && b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
        
        var tomorrowBookings = await _context.Bookings
            .Where(b => b.StartTime.Date == tomorrow && b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
        
        // Get future bookings for the remaining schedule
        var upcomingFutureBookings = await _context.Bookings
            .Where(b => b.StartTime.Date > tomorrow && b.Status == BookingStatus.Reserved)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
        
        var viewModel = new BookingIndexViewModel
        {
            CurrentBooking = currentBooking,
            TodayBookings = todayBookings,
            TomorrowBookings = tomorrowBookings,
            RemainingBookings = upcomingFutureBookings,
            NewBooking = new Booking
            {
                StartTime = DateTime.Now.AddMinutes(5).RoundToNearest(TimeSpan.FromMinutes(15)),
                EndTime = DateTime.Now.AddMinutes(65).RoundToNearest(TimeSpan.FromMinutes(15))
            }
        };
        
        return View(viewModel);
    }
    
    // Check in
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            TempData["ErrorMessage"] = "Rezervacija ni bila najdena.";
            return RedirectToAction(nameof(Index));
        }
        
        booking.Status = BookingStatus.CheckedIn;
        booking.CheckInTime = DateTime.Now;
        await _context.SaveChangesAsync();
        
        // Send updated booking data to clients
        await SendBookingUpdateToClients();
        
        // Don't set success message
        return RedirectToAction(nameof(Index));
    }
    
    // Check out
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            TempData["ErrorMessage"] = "Rezervacija ni bila najdena.";
            return RedirectToAction(nameof(Index));
        }
        
        booking.Status = BookingStatus.CheckedOut;
        booking.CheckOutTime = DateTime.Now;
        await _context.SaveChangesAsync();
        
        // Send updated booking data to clients
        await SendBookingUpdateToClients();
        
        // Don't set success message
        return RedirectToAction(nameof(Index));
    }
    
    // Cancel booking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            TempData["ErrorMessage"] = "Rezervacija ni bila najdena.";
            return RedirectToAction(nameof(Index));
        }
        
        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();
        
        // Send updated booking data to clients
        await SendBookingUpdateToClients();
        
        // Don't set success message
        return RedirectToAction(nameof(Index));
    }
    
    // Admin dashboard - requires authorization
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var bookings = await _context.Bookings
            .AsNoTracking()  // Use AsNoTracking for better performance in read-only scenarios
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
            
        return View(bookings);
    }
    
    // Helper method to get current booking
    private async Task<Booking> GetCurrentBookingAsync()
    {
        var now = DateTime.Now;
        return await _context.Bookings
            .Where(b => b.StartTime <= now && b.EndTime > now && 
                    (b.Status == BookingStatus.Reserved || b.Status == BookingStatus.CheckedIn))
            .OrderBy(b => b.StartTime)
            .FirstOrDefaultAsync();
    }

    // Direct booking from form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBooking(string studentName, DateTime startTime, DateTime endTime, string reason)
    {
        if (string.IsNullOrWhiteSpace(studentName))
        {
            TempData["ErrorMessage"] = "Ime dijaka je obvezno";
            return RedirectToAction(nameof(Index));
        }
        
        if (startTime >= endTime)
        {
            TempData["ErrorMessage"] = "Končni čas mora biti po začetnem času";
            return RedirectToAction(nameof(Index));
        }
        
        // Validate time is in the future
        if (startTime < DateTime.Now)
        {
            TempData["ErrorMessage"] = "Začetni čas ne more biti v preteklosti";
            return RedirectToAction(nameof(Index));
        }
        
        try
        {
            // Check for overlapping bookings
            var overlapping = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled && 
                            b.Status != BookingStatus.CheckedOut &&
                           ((b.StartTime <= startTime && b.EndTime > startTime) ||
                            (b.StartTime < endTime && b.EndTime >= endTime) ||
                            (b.StartTime >= startTime && b.EndTime <= endTime)))
                .AnyAsync();

            if (overlapping)
            {
                TempData["ErrorMessage"] = "Izbrani časovni termin se prekriva z obstoječo rezervacijo.";
                return RedirectToAction(nameof(Index));
            }

            var booking = new Booking
            {
                StudentName = studentName,
                Reason = reason,
                StartTime = startTime,
                EndTime = endTime,
                CreatedAt = DateTime.Now,
                Status = BookingStatus.Reserved
            };
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            
            // Send updated booking data to clients
            await SendBookingUpdateToClients();
            
            // Don't set success message
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Napaka pri ustvarjanju rezervacije: " + ex.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    // Helper method to send updated booking data to all clients
    private async Task SendBookingUpdateToClients()
    {
        var currentBooking = await GetCurrentBookingAsync();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        
        // Get today's bookings including active ones (don't filter by StartTime > DateTime.Now)
        var todayBookings = await _context.Bookings
            .Where(b => b.StartTime.Date == today && 
                    b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .Select(b => new {
                id = b.Id,
                studentName = b.StudentName,
                reason = b.Reason,
                startTime = b.StartTime.ToString("HH:mm"),
                endTime = b.EndTime.ToString("HH:mm"),
                status = (int)b.Status
            })
            .ToListAsync();

        var tomorrowBookings = await _context.Bookings
            .Where(b => b.StartTime.Date == tomorrow && b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .Select(b => new {
                id = b.Id,
                studentName = b.StudentName,
                reason = b.Reason,
                startTime = b.StartTime.ToString("HH:mm"),
                endTime = b.EndTime.ToString("HH:mm"),
                status = (int)b.Status
            })
            .ToListAsync();
        
        var data = new {
            hasCurrentBooking = currentBooking != null,
            currentBookingStatus = currentBooking != null ? (int)currentBooking.Status : -1,
            currentBooking = currentBooking != null ? new {
                id = currentBooking.Id,
                studentName = currentBooking.StudentName,
                reason = currentBooking.Reason,
                startTime = currentBooking.StartTime.ToString("HH:mm"),
                endTime = currentBooking.EndTime.ToString("HH:mm"),
                status = (int)currentBooking.Status
            } : null,
            todayBookings = todayBookings,
            tomorrowBookings = tomorrowBookings
        };
        
        await _hubContext.Clients.All.SendAsync("RefreshBookingStatus", data);
    }
    
    // API endpoint to check for upcoming bookings
    [HttpGet]
    public async Task<IActionResult> CheckUpcomingBookings()
    {
        var now = DateTime.Now;
        
        // Check if there's a current booking that needs attention
        var currentBooking = await GetCurrentBookingAsync();

        // If there's a current booking that just started or is active but not checked in
        if (currentBooking != null && currentBooking.Status == BookingStatus.Reserved)
        {
            // If the booking just started (within the last minute)
            if (currentBooking.StartTime <= now && currentBooking.StartTime >= now.AddMinutes(-1))
            {
                // This is a booking that just started - notify clients to update UI
                await _hubContext.Clients.All.SendAsync("BookingStarted", new {
                    id = currentBooking.Id,
                    studentName = currentBooking.StudentName,
                    reason = currentBooking.Reason,
                    startTime = currentBooking.StartTime.ToString("HH:mm"),
                    endTime = currentBooking.EndTime.ToString("HH:mm"),
                    status = (int)currentBooking.Status
                });
            }
            
            // Always send a BookingRequiresAction event for any active but not checked-in booking
            await _hubContext.Clients.All.SendAsync("BookingRequiresAction", new {
                id = currentBooking.Id,
                studentName = currentBooking.StudentName,
                reason = currentBooking.Reason,
                startTime = currentBooking.StartTime.ToString("HH:mm"),
                endTime = currentBooking.EndTime.ToString("HH:mm"),
                status = (int)currentBooking.Status
            });
        }
        
        // Check if there's a booking coming up within the next 5 minutes
        var upcomingBookingSoon = await _context.Bookings
            .Where(b => b.StartTime > now && 
                      b.StartTime <= now.AddMinutes(5) && 
                      b.Status == BookingStatus.Reserved)
            .OrderBy(b => b.StartTime)
            .FirstOrDefaultAsync();
            
        // If there's an upcoming booking, notify clients
        if (upcomingBookingSoon != null)
        {
            // Calculate time remaining in seconds
            var timeRemaining = (int)(upcomingBookingSoon.StartTime - now).TotalSeconds;
            
            // If very close to start time (within 10 seconds), consider it started
            if (timeRemaining <= 10)
            {
                await _hubContext.Clients.All.SendAsync("BookingStarted", new {
                    id = upcomingBookingSoon.Id,
                    studentName = upcomingBookingSoon.StudentName,
                    reason = upcomingBookingSoon.Reason,
                    startTime = upcomingBookingSoon.StartTime.ToString("HH:mm"),
                    endTime = upcomingBookingSoon.EndTime.ToString("HH:mm"),
                    status = (int)upcomingBookingSoon.Status
                });
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("UpcomingBookingAlert", new {
                    id = upcomingBookingSoon.Id,
                    studentName = upcomingBookingSoon.StudentName,
                    reason = upcomingBookingSoon.Reason,
                    startTime = upcomingBookingSoon.StartTime.ToString("HH:mm"),
                    endTime = upcomingBookingSoon.EndTime.ToString("HH:mm"),
                    timeRemaining = timeRemaining,
                    status = (int)upcomingBookingSoon.Status
                });
            }
        }
        
        return Json(new { success = true });
    }
    
    // Method that will be called by a hosted background service to regularly check for upcoming bookings
    public async Task CheckAndNotifyUpcomingBookings()
    {
        var result = await CheckUpcomingBookings();
        
        // Also send the current booking state for real-time updates
        await SendBookingUpdateToClients();
    }
} 