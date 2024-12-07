namespace Application.Interfaces;

public interface IGitHubSocialService
{
    void Configure(string userApiToken);

    Task FetchFollowersAndFollowing();

    Task UnfollowUsersNotFollowingBack();

    Task FollowUsersNotFollowedBack();

    Task<List<string>> ScrapeFollowersOfFollowers();

    Task<List<string>> ScrapeFollowersOfFollowers(int targetCount);

    Task FollowPotentialFollowers(List<string> potentialFollowers);

    Task FollowPotentialFollowers(List<string> potentialFollowers, int num);
}