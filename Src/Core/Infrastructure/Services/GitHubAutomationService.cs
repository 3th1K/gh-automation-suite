using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Infrastructure.Services;

public class GitHubAutomationService : IGitHubAutomationService
{
    private readonly IGitHubContributionService _contributionService;
    private readonly IGitHubSocialService _socialService;
    private readonly ILogger<GitHubAutomationService> _logger;

    public GitHubAutomationService(ILogger<GitHubAutomationService> logger, IGitHubContributionService contributionService, IGitHubSocialService socialService)
    {
        _logger = logger;
        _contributionService = contributionService;
        _socialService = socialService;
    }

    public async Task AutomateGitHubContributionAsync(string userApiToken, string repositoryName)
    {
        _logger.LogInformation("Automated contribution started!");
        _contributionService.Configure(userApiToken, repositoryName);
        var repository = await _contributionService.EnsureRepositoryExistsAsync();

        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.txt";
        var fileContent = await _contributionService.EnsureFileExistsAsync(repository, fileName);

        var updatedContent = fileContent + "\n" + _contributionService.GenerateGibberishText();

        await _contributionService.CommitChangesAsync(repository, fileName, updatedContent);

        _logger.LogInformation("Automated contribution successfully completed!");
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
}