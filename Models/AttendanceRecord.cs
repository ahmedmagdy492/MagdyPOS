using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models;

public sealed class AttendanceRecord
{
    public long Id { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Local date (server local time) for weekly grouping.
    /// </summary>
    public DateTime WorkDate { get; set; }

    public DateTime CheckInAtUtc { get; set; }

    public DateTime? CheckOutAtUtc { get; set; }

    [MaxLength(400)]
    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

