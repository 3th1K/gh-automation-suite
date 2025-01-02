using System.ComponentModel.DataAnnotations;

namespace Domain.Requests;

public class JobRequest
{
    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public string RepositoryName { get; set; } = null!;

    [Required]
    public int IntervalInMinutes { get; set; }
}