using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Infrastructure.Services;

public class GitHubSocialService : IGitHubSocialService
{
    private List<string> _followers = [];
    private List<string> _followings = [];
    private List<string> _unfollowedUsers = [];
    private string _userApiToken = string.Empty;

    private readonly ILogger<GitHubSocialService> _logger;
    private readonly GitHubClient _client;

    public GitHubSocialService(ILogger<GitHubSocialService> logger, GitHubClient client)
    {
        _logger = logger;
        _client = client;
    }

    public void Configure(string userApiToken)
    {
        _userApiToken = userApiToken;

        _client.Credentials = new Credentials(_userApiToken);
    }

    public async Task FetchFollowersAndFollowing()
    {
        _logger.LogInformation("Fetching the followers and the followings");
        var followers = await _client.User.Followers.GetAllForCurrent();
        var following = await _client.User.Followers.GetAllFollowingForCurrent();
        _followers = followers.Select(f => f.Login).ToList();
        _followings = following.Select(f => f.Login).ToList();
        _logger.LogInformation($"Followers - {_followers.Count}, Followings - {_followings.Count}");
        await LogRate();
    }

    public async Task UnfollowUsersNotFollowingBack()
    {
        Console.WriteLine("Unfollowing the users who arent following back");
        var notFollowingBack = _followings.Except(_followers).ToList();
        foreach (var user in notFollowingBack)
        {
            try
            {
                await _client.User.Followers.Unfollow(user);
                Console.WriteLine($"Unfollowed: {user}");
                _followings.Remove(user);
                _unfollowedUsers.Add(user);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to Unfollow: {user}, Error : {e.Message}");
            }
        }
        Console.WriteLine($"Followers - {_followers.Count}, Followings - {_followings.Count}");
        await LogRate();
    }

    public async Task FollowUsersNotFollowedBack()
    {
        _logger.LogInformation("Following users that follows");
        var notFollowedBack = _followers.Except(_followings).ToList();
        foreach (var user in notFollowedBack)
        {
            bool success = await _client.User.Followers.Follow(user);
            if (success)
            {
                _logger.LogInformation($"Followed: {user}");
                _followings.Add(user);
            }
            else
            {
                _logger.LogInformation($"Unable to Follow: {user}");
            }
        }
        _logger.LogInformation($"Followers - {_followers.Count}, Followings - {_followings.Count}");
        await LogRate();
    }

    public async Task<List<string>> ScrapeFollowersOfFollowers()
    {
        _logger.LogInformation("Scraping users to follow");
        var potentialFollowers = new HashSet<string>();
        foreach (var follower in _followers)
        {
            var followerFollowers = await _client.User.Followers.GetAll(follower); //_gitHubApiService.GetFollowersAsync(follower);
            foreach (var f in followerFollowers.Select(f => f.Login))
            {
                if (!_followings.Contains(f) && !_followers.Contains(f))
                {
                    potentialFollowers.Add(f);
                }
            }
        }
        _logger.LogInformation($"Found {potentialFollowers.Count} users to follow");
        await LogRate();
        return [.. potentialFollowers];
    }

    public async Task<List<string>> ScrapeFollowersOfFollowers(int targetCount)
    {
        _logger.LogInformation($"Scraping users to follow, potential followers {targetCount}");
        var potentialFollowers = new HashSet<string>(); // Avoid duplicates
        var visitedUsers = new HashSet<string>(_unfollowedUsers); // Initialize with known followers and recently unfollowed users

        var queue = new Queue<string>(_followers); // Start with existing followers

        // Ensure the target count respects the initial known list size
        foreach (var follower in _followers)
        {
            if (!_followings.Contains(follower))
            {
                potentialFollowers.Add(follower);
                if (potentialFollowers.Count >= targetCount)
                    return potentialFollowers.ToList();
            }
        }

        // Continue expanding from followers' followers
        while (queue.Count > 0 && potentialFollowers.Count < targetCount)
        {
            var currentUser = queue.Dequeue();

            // Skip already visited users
            if (visitedUsers.Contains(currentUser))
                continue;

            visitedUsers.Add(currentUser); // Mark as visited

            try
            {
                // Fetch followers of the current user only for new users
                var currentFollowers = await _client.User.Followers.GetAll(currentUser); //_gitHubApiService.GetFollowersAsync(currentUser);

                foreach (var follower in currentFollowers.Select(f => f.Login))
                {
                    if (!_followings.Contains(follower) && !_followers.Contains(follower) && !potentialFollowers.Contains(follower) && !_unfollowedUsers.Contains(follower))
                    {
                        potentialFollowers.Add(follower);

                        // Stop if the target count is reached
                        if (potentialFollowers.Count >= targetCount)
                        {
                            _logger.LogInformation($"Found {potentialFollowers.Count} users to follow");
                            await LogRate();
                            return potentialFollowers.ToList();
                        }
                    }

                    // Add new follower to the queue for further exploration
                    if (!visitedUsers.Contains(follower))
                    {
                        queue.Enqueue(follower);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching followers for {currentUser}: {ex.Message}");
            }
        }
        _logger.LogInformation($"Found {potentialFollowers.Count} users to follow");
        await LogRate();
        return potentialFollowers.ToList();
    }

    public async Task FollowPotentialFollowers(List<string> potentialFollowers)
    {
        _logger.LogInformation("Following potential followers");
        foreach (var newUser in potentialFollowers)
        {
            bool success = await _client.User.Followers.Follow(newUser); //_gitHubApiService.FollowUserAsync(newUser);
            if (success)
            {
                _logger.LogInformation($"Followed: {newUser}");
                await LogRate();
                _followings.Add(newUser);
            }
            else
            {
                _logger.LogError($"Unable to Follow: {newUser}");
                await LogRate();
            }
        }
        //await _gitHubApiService.LogRateLimitAsync();
    }

    public async Task FollowPotentialFollowers(List<string> potentialFollowers, int num)
    {
        foreach (var newUser in potentialFollowers.Take(num))
        {
            try
            {
                bool success = await _client.User.Followers.Follow(newUser);//_gitHubApiService.FollowUserAsync(newUser);
                if (success)
                {
                    _logger.LogInformation($"Followed: {newUser}");
                    await LogRate();
                    _followings.Add(newUser);
                }
                else
                {
                    _logger.LogError($"Unable to Follow: {newUser}");
                    await LogRate();
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e.Message}");
            }
        }

        //await _gitHubApiService.LogRateLimitAsync();
    }

    private async Task LogRate()
    {
        var rateLimit = await _client.RateLimit.GetRateLimits();
        var rate = rateLimit.Rate;
        var remainingReq = rate.Remaining;
        var resetTime = rate.Reset;
        var limit = rate.Limit;

        _logger.LogInformation($"Request Limit: {limit}, Remaining Requests: {remainingReq}, Reset Time: {resetTime}");
    }
}