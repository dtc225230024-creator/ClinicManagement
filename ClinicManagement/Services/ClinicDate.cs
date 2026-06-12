namespace ClinicManagement.Services;

public static class ClinicDate
{
    public static DateTime Today => DateTime.UtcNow.AddHours(7).Date;
    public static DateTime Now => DateTime.UtcNow.AddHours(7);
}
