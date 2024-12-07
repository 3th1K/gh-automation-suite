namespace Application.Interfaces;

public interface IGitHubAutomationService
{
    Task AutomateGitHubContributionAsync(string userApiToken, string repositoryName);

    Task AutomateGitHubSocialBoostAsync(string userApiToken, bool unfollowUsersNotFollowingBack = true, bool followUsersNotFollowedBack = true);

    Task AutomateGitHubSocialBoostAsync(string userApiToken, int count = 100, bool unfollowUsersNotFollowingBack = true, bool followUsersNotFollowedBack = true);
}