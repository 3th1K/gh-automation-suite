namespace Domain.Models;

public class ScheduledJob
{ 
    public string Token { get; set; } = null!;
    public int IntervalInMinutes { get; set; }
    public string RepositoryName { get; set; } = null!;
    public string JobStatus { get; set; } = null!;
}