namespace MagdyPOS.Helpers;


public static class Utils {
    public static string GetShiftLabel(DateTime date) {
        DateTimeOffset localTime = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
    
        int hour = localTime.Hour;
        
        if (hour >= 7 && hour < 17)
            return "وردية: صباحية";
        
        return "وردية: مسائية";
    }
}