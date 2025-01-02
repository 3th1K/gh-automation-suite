using Application.Interfaces;
using Domain.Models;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Infrastructure.Services;

public class GitHubAutomationService : IGitHubAutomationService
{
    private readonly IGitHubContributionService _contributionService;
    private readonly IGitHubSocialService _socialService;
    private readonly ILogger<GitHubAutomationService> _logger;
    private static readonly ConcurrentDictionary<string, (int, string)> ActiveJobs = new();

    public GitHubAutomationService(ILogger<GitHubAutomationService> logger, IGitHubContributionService contributionService, IGitHubSocialService socialService)
    {
        _logger = logger;
        _contributionService = contributionService;
        _socialService = socialService;
    }

    public async Task AutomateGitHubContributionAsync(string userApiToken, string repositoryName)
    {
        _logger.LogInformation("Automated contribution started!");
        await _contributionService.Configure(userApiToken, repositoryName);
        var repository = await _contributionService.EnsureRepositoryExistsAsync();

        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.txt";
        var fileContent = await _contributionService.EnsureFileExistsAsync(repository, fileName);

        var updatedContent = fileContent + "\n" + _contributionService.GenerateGibberishText();

        await _contributionService.CommitChangesAsync(repository, fileName, updatedContent);

        _logger.LogInformation("Automated contribution successfully completed!");
    }

    public void ScheduleGitHubContribution(string userApiToken, string repositoryName, int intervalInMinutes)
    {
        if (string.IsNullOrWhiteSpace(userApiToken) || intervalInMinutes <= 0)
        {
            throw new ArgumentNullException("Invalid token or interval");
        }

        if (ActiveJobs.ContainsKey(userApiToken))
        {
            throw new ArgumentException($"A job for token '{userApiToken}' is already scheduled.");
        }

        BackgroundJob.Enqueue(() => AutomateGitHubContributionAsync(userApiToken, repositoryName));

        string cronExpression = GetCronExpression(intervalInMinutes);

        RecurringJob.AddOrUpdate(
            userApiToken,
            () => AutomateGitHubContributionAsync(userApiToken, repositoryName),
            cronExpression);

        ActiveJobs.TryAdd(userApiToken, (intervalInMinutes, repositoryName));
    }
    public bool UnscheduleGitHubContribution(string userApiToken)
    {
        if (string.IsNullOrWhiteSpace(userApiToken))
        {
            throw new ArgumentNullException("Invalid token.");
        }

        if (!ActiveJobs.ContainsKey(userApiToken))
        {
            return false;
        }

        // Remove the recurring job
        RecurringJob.RemoveIfExists(userApiToken);

        // Remove from tracking
        ActiveJobs.TryRemove(userApiToken, out _);

        return true;
    }

    public ScheduledJob CheckScheduled(string userApiToken) 
    {
        if (string.IsNullOrWhiteSpace(userApiToken))
        {
            throw new ArgumentNullException("Invalid token.");
        }

        if (ActiveJobs.TryGetValue(userApiToken, out (int, string) interval_repo))
        {
            return new ScheduledJob { Token = userApiToken, IntervalInMinutes = interval_repo.Item1, RepositoryName = interval_repo.Item2, JobStatus = "Scheduled" };
        }
        return new ScheduledJob { Token = userApiToken, IntervalInMinutes = interval_repo.Item1, RepositoryName = interval_repo.Item2, JobStatus = "Not Found" };
    }


    public async Task AutomateGitHubSocialBoostAsync(string userApiToken, bool unfollowUsersNotFollowingBack = true, bool followUsersNotFollowedBack = true)
    {
        _logger.LogInformation("Automated social boost started!");
        _socialService.Configure(userApiToken);

        await _socialService.FetchFollowersAndFollowing();

        if (unfollowUsersNotFollowingBack)
            await _socialService.UnfollowUsersNotFollowingBack();

        if (followUsersNotFollowedBack)
            await _socialService.FollowUsersNotFollowedBack();

        List<string> potentialFollowers = await _socialService.ScrapeFollowersOfFollowers();

        await _socialService.FollowPotentialFollowers(potentialFollowers);
        _logger.LogInformation("Automated social boost completed successfully!");
    }

    public async Task AutomateGitHubSocialBoostAsync(string userApiToken, int count = 100, bool unfollowUsersNotFollowingBack = true, bool followUsersNotFollowedBack = true)
    {
        _logger.LogInformation("Automated social boost started!");
        _socialService.Configure(userApiToken);

        await _socialService.FetchFollowersAndFollowing();

        if (unfollowUsersNotFollowingBack)
            await _socialService.UnfollowUsersNotFollowingBack();

        if (followUsersNotFollowedBack)
            await _socialService.FollowUsersNotFollowedBack();

        List<string> potentialFollowers = [];
        if (count > 0)
            potentialFollowers = await _socialService.ScrapeFollowersOfFollowers(count);
        else
            potentialFollowers = await _socialService.ScrapeFollowersOfFollowers();

        await _socialService.FollowPotentialFollowers(potentialFollowers);
        _logger.LogInformation("Automated social boost completed successfully!");
    }

    // Helper method to generate a valid cron expression
    private string GetCronExpression(int intervalInMinutes)
    {
        if (intervalInMinutes < 60)
        {
            // If the interval is less than 60 minutes, run every X minutes
            return $"*/{intervalInMinutes} * * * *";
        }
        else if (intervalInMinutes == 1440)
        {
            // If the interval is 1440 minutes (24 hours), run once a day
            return "0 0 * * *";
        }
        else
        {
            // For other cases, use the general cron format for intervals >= 60
            int hours = intervalInMinutes / 60;
            int minutes = intervalInMinutes % 60;
            return $"{minutes} {hours} * * *";
        }
    }
}