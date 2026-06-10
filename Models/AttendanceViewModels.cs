namespace MagdyPOS.Models;

public sealed class MyAttendanceViewModel
{
    public DateTime WorkDate { get; init; }
    public AttendanceRecord? TodayRecord { get; init; }
}

public sealed class AttendanceWeekDayColumnViewModel
{
    public DateTime Date { get; init; }
    public string Label { get; init; } = string.Empty; // e.g. "Sat 27"
}

public sealed class AttendanceWeekUserRowViewModel
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? FullName { get; init; }

    public IReadOnlyDictionary<DateTime, AttendanceRecord> RecordsByDate { get; init; } = new Dictionary<DateTime, AttendanceRecord>();
}

public sealed class AttendanceWeekReportViewModel
{
    public DateTime WeekStart { get; init; } // inclusive (date)
    public DateTime WeekEnd { get; init; }   // exclusive (date)
    public IReadOnlyList<AttendanceWeekDayColumnViewModel> Days { get; init; } = Array.Empty<AttendanceWeekDayColumnViewModel>();
    public IReadOnlyList<AttendanceWeekUserRowViewModel> Users { get; init; } = Array.Empty<AttendanceWeekUserRowViewModel>();
}

