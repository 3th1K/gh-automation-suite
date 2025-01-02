using Domain.Models;

namespace Application.Interfaces;

public interface IGitHubAutomationService
{
    Task AutomateGitHubContributionAsync(string userApiToken, string repositoryName);

    Task AutomateGitHubSocialBoostAsync(string userApiToken, bool unfollowUsersNotFollowingBack = true, bool followUsersNotFollowedBack = true);

    Task AutomateGitHubSocialBoostAsync(string userApiToken, int count = 100, bool unfollowUsersNotFollowingBack = true, bool followUsersNotFollowedBack = true);

    void ScheduleGitHubContribution(string userApiToken, string repositoryName, int intervalInMinutes);

    bool UnscheduleGitHubContribution(string userApiToken);

    ScheduledJob CheckScheduled(string userApiToken);
}