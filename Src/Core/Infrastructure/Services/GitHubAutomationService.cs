using Application.Interfaces;
using System;
using System.Text;

namespace Infrastructure.Services;

public class GitHubAutomationService : IGitHubAutomationService
{
    private readonly IGitHubContributionService _contributionService;
    private readonly IGitHubSocialService _socialService;

    public GitHubAutomationService(IGitHubContributionService contributionService, IGitHubSocialService socialService)
    {
        _contributionService = contributionService;
        _socialService = socialService;
    }

    public async Task AutomateGitHubContributionAsync(string userApiToken, string repositoryName)
    {
        _contributionService.Configure(userApiToken, repositoryName);
        var repository = await _contributionService.EnsureRepositoryExistsAsync();

        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.txt";
        var fileContent = await _contributionService.EnsureFileExistsAsync(repository, fileName);

        var updatedContent = fileContent + "\n" + _contributionService.GenerateGibberishText();

        await _contributionService.CommitChangesAsync(repository, fileName, updatedContent);

        Console.WriteLine("Automated contribution successfully completed!");
    }

    public async Task AutomateGitHubSocialBoostAsync(string userApiToken)
    {
        _socialService.Configure(userApiToken);
        // Step 1: Fetch followers and following
        await _socialService.FetchFollowersAndFollowing();

        // Step 2: Find users to unfollow
        await _socialService.UnfollowUsersNotFollowingBack();

        // Step 3: Find users to follow back
        await _socialService.FollowUsersNotFollowedBack();

        // Step 4: Scrape followers of followers
        List<string> potentialFollowers = await _socialService.ScrapeFollowersOfFollowers();

        // Step 5: Follow potential followers
        await _socialService.FollowPotentialFollowers(potentialFollowers);
    }

    public async Task AutomateGitHubSocialBoostAsync(string userApiToken, int num = 100)
    {
        _socialService.Configure(userApiToken);
        // Step 1: Fetch followers and following
        await _socialService.FetchFollowersAndFollowing();

        // Step 2: Find users to unfollow
        await _socialService.UnfollowUsersNotFollowingBack();

        // Step 3: Find users to follow back
        await _socialService.FollowUsersNotFollowedBack();

        // Step 4: Scrape followers of followers
        List<string> potentialFollowers = [];
        if (num > 0)
            potentialFollowers = await _socialService.ScrapeFollowersOfFollowers(num);
        else
            potentialFollowers = await _socialService.ScrapeFollowersOfFollowers();

        // Step 5: Follow potential followers
        await _socialService.FollowPotentialFollowers(potentialFollowers);
    }
}