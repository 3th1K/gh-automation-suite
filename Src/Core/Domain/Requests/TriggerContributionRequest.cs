using System.ComponentModel.DataAnnotations;

namespace Domain.Requests
{
    public class TriggerContributionRequest
    {
        [Required]
        public string Username { get; init; } = null!;

        [Required]
        public string Token { get; init; } = null!;

        [Required]
        public string RepositoryName { get; init; } = null!;
    }
}