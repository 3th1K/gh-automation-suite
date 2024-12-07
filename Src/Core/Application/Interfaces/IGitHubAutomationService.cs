namespace Application.Interfaces;

public interface IGitHubAutomationService
{
    Task AutomateGitHubContributionAsync(string userApiToken, string repositoryName);

    Task AutomateGitHubSocialBoostAsync(string userApiToken);

    Task AutomateGitHubSocialBoostAsync(string userApiToken, int num = 100);
}