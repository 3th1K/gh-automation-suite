namespace Application.Interfaces;

public interface IGitHubAutomationService
{
    void Configure(string userName, string userApiToken, string repositoryName);

    Task AutomateGitHubContributionAsync();
}