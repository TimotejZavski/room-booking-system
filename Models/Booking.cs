using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Booking
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Ime dijaka je obvezno")]
    [Display(Name = "Ime dijaka")]
    public string? StudentName { get; set; }
    
    [Display(Name = "Razlog")]
    [Required(ErrorMessage = "Razlog je obvezen")]
    public string? Reason { get; set; }
    
    [Required(ErrorMessage = "Začetni čas je obvezen")]
    [Display(Name = "Začetni čas")]
    public DateTime StartTime { get; set; }
    
    [Required(ErrorMessage = "Končni čas je obvezen")]
    [Display(Name = "Končni čas")]
    public DateTime EndTime { get; set; }
    
    [Display(Name = "Ustvarjeno")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Display(Name = "Čas prijave")]
    public DateTime? CheckInTime { get; set; }
    
    [Display(Name = "Čas odjave")]
    public DateTime? CheckOutTime { get; set; }
    
    [Display(Name = "Status")]
    public BookingStatus Status { get; set; } = BookingStatus.Reserved;
}

public enum BookingStatus
{
    Reserved,
    CheckedIn,
    CheckedOut,
    Cancelled
} 