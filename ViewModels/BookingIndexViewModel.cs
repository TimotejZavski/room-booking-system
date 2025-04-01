using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class BookingIndexViewModel
{
    public Booking CurrentBooking { get; set; }
    public IEnumerable<Booking> UpcomingBookings { get; set; } = new List<Booking>();
    public IEnumerable<Booking> TodayBookings { get; set; } = new List<Booking>();
    public IEnumerable<Booking> TomorrowBookings { get; set; } = new List<Booking>();
    public IEnumerable<Booking> RemainingBookings { get; set; } = new List<Booking>();
    public Booking NewBooking { get; set; } = new Booking();
} 