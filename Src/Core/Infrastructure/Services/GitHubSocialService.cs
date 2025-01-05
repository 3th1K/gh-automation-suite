using Application.Interfaces;
using Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Collections.Concurrent;

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

        // Run the tasks in parallel
        var followersTask = _client.User.Followers.GetAllForCurrent();
        var followingTask = _client.User.Followers.GetAllFollowingForCurrent();

        // Await both tasks to complete
        await Task.WhenAll(followersTask, followingTask);

        // Retrieve the results
        var followers = await followersTask;
        var following = await followingTask;

        // Process the results
        _followers = followers.Select(f => f.Login).ToList();
        _followings = following.Select(f => f.Login).ToList();

        _logger.LogInformation($"Followers - {_followers.Count}, Followings - {_followings.Count}");

        await LogRate();
    }


    public async Task UnfollowUsersNotFollowingBack()
    {
        _logger.LogInformation("Unfollowing the users who aren't following back");
        var notFollowingBack = _followings.Except(_followers).ToList();
        _logger.LogInformation($"These users ({notFollowingBack.Count}) are not following back: {string.Join(", ", notFollowingBack)}");

        await Parallel.ForEachAsync(notFollowingBack, async (user, cancellationToken) =>
        {
            try
            {
                await _client.User.Followers.Unfollow(user);
                _logger.LogInformation($"Unfollowed: {user}");
                _followings.Remove(user);
                _unfollowedUsers.Add(user);
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to unfollow: {user}, Error: {e.Message}");
            }
        });

        _logger.LogInformation($"Followers - {_followers.Count}, Followings - {_followings.Count}");
        await LogRate();
    }


    public async Task FollowUsersNotFollowedBack()
    {
        _logger.LogInformation("Following users who aren't being followed back");
        var notFollowedBack = _followers.Except(_followings).ToList();
        _logger.LogInformation($"These users ({notFollowedBack.Count}) are not being followed back: {string.Join(", ", notFollowedBack)}");

        await Parallel.ForEachAsync(notFollowedBack, async (user, cancellationToken) =>
        {
            try
            {
                bool success = await _client.User.Followers.Follow(user);
                if (success)
                {
                    _logger.LogInformation($"Followed: {user}");
                    _followings.Add(user);
                }
                else
                {
                    _logger.LogInformation($"Unable to follow: {user}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error following {user}: {e.Message}");
            }
        });

        _logger.LogInformation($"Followers - {_followers.Count}, Followings - {_followings.Count}");
        await LogRate();
    }


    public async Task<List<string>> ScrapeFollowersOfFollowers()
    {
        _logger.LogInformation("Scraping users to follow");
        var potentialFollowers = new ConcurrentHashSet<string>();

        await Parallel.ForEachAsync(_followers, async (follower, cancellationToken) =>
        {
            var followerFollowers = await _client.User.Followers.GetAll(follower);
            foreach (var f in followerFollowers.Select(ff => ff.Login))
            {
                if (!_followings.Contains(f) && !_followers.Contains(f))
                {
                    potentialFollowers.Add(f);
                }
            }
        });

        _logger.LogInformation($"Found {potentialFollowers.Count} users to follow");
        await LogRate();
        return potentialFollowers.ToList();
    }


    public async Task<List<string>> ScrapeFollowersOfFollowers(int targetCount)
    {
        _logger.LogInformation($"Scraping users to follow, target count: {targetCount}");
        var potentialFollowers = new ConcurrentHashSet<string>(); // Avoid duplicates
        var visitedUsers = new ConcurrentHashSet<string>(_unfollowedUsers); // Initialize with known followers and recently unfollowed users
        var queue = new ConcurrentQueue<string>(_followers); // Start with existing followers

        // Add direct followers not in followings to potential followers
        foreach (var follower in _followers)
        {
            if (!_followings.Contains(follower))
            {
                potentialFollowers.Add(follower);
                if (potentialFollowers.Count >= targetCount)
                    return potentialFollowers.ToList();
            }
        }

        // Use Parallel.ForEachAsync to process users concurrently
        var stopProcessing = false; // Flag to stop processing once the target is reached
        await Parallel.ForEachAsync(queue, async (currentUser, cancellationToken) =>
        {
            if (stopProcessing) return; // Stop processing if flag is set

            if (visitedUsers.Contains(currentUser))
                return;

            visitedUsers.Add(currentUser); // Mark as visited

            try
            {
                var currentFollowers = await _client.User.Followers.GetAll(currentUser);

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
                            stopProcessing = true; // Set the flag to stop further processing
                            return; // Exit early from the parallel loop
                        }
                    }

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
        });

        _logger.LogInformation($"Found {potentialFollowers.Count} users to follow");
        await LogRate();
        return potentialFollowers.ToList();
    }




    public async Task FollowPotentialFollowers(List<string> potentialFollowers)
    {
        _logger.LogInformation("Following potential followers");

        await Parallel.ForEachAsync(potentialFollowers, async (newUser, cancellationToken) =>
        {
            try
            {
                bool success = await _client.User.Followers.Follow(newUser);
                if (success)
                {
                    _logger.LogInformation($"Followed: {newUser}");
                    _followings.Add(newUser);
                }
                else
                {
                    _logger.LogError($"Unable to Follow: {newUser}");
                }
                await LogRate();
            }
            catch (Exception e)
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             {
                _logger.LogError($"Error following {newUser}: {e.Message}");
            }
        });

        _logger.LogInformation("Finished following potential followers");
    }


    public async Task FollowPotentialFollowers(List<string> potentialFollowers, int num)
    {
        var usersToFollow = potentialFollowers.Take(num).ToList();

        _logger.LogInformation($"Following up to {num} potential followers");

        await Parallel.ForEachAsync(usersToFollow, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (newUser, cancellationToken) =>
        {
            try
            {
                bool success = await _client.User.Followers.Follow(newUser);
                if (success)
                {
                    _logger.LogInformation($"Followed: {newUser}");
                    _followings.Add(newUser);
                }
                else
                {
                    _logger.LogError($"Unable to Follow: {newUser}");
                }
                await LogRate();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error following {newUser}: {e.Message}");
            }
        });

        _logger.LogInformation("Finished following potential followers");
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
