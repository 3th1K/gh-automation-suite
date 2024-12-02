using Application.Interfaces;
using Domain.Models;

namespace Infrastructure.Services;

public class GitHubFollowerManagerService : IGitHubFollowerManagerService
{
    private readonly IGitHubApiService _gitHubApiService;
    private string Username;
    private List<string> Followers = [];
    private List<string> Followings = [];

    public GitHubFollowerManagerService(IGitHubApiService gitHubApiService, string username)
    {
        _gitHubApiService = gitHubApiService;
        Username = username;
    }

    public async Task FetchFollowersAndFollowing()
    {
        Console.WriteLine("Fetching the followers and the followings");
        var followers = await _gitHubApiService.GetFollowersAsync(Username);
        var following = await _gitHubApiService.GetFollowingAsync(Username);
        Followers = followers;
        Followings = following;
        Console.WriteLine($"Followers - {Followers.Count}, Followings - {Followings.Count}");
    }

    public async Task UnfollowUsersNotFollowingBack()
    {
        Console.WriteLine("Unfollowing the users who arent following back");
        var notFollowingBack = Followings.Except(Followers).ToList();
        foreach (var user in notFollowingBack)
        {
            bool success = await _gitHubApiService.UnfollowUserAsync(user);
            if (success)
            {
                Console.WriteLine($"Unfollowed: {user}");
                Followings.Remove(user);
            }
            else
            {
                Console.WriteLine($"Unable to Unfollow: {user}");
            }
        }
        Console.WriteLine($"Followers - {Followers.Count}, Followings - {Followings.Count}");
    }

    public async Task FollowUsersNotFollowedBack()
    {
        Console.WriteLine("Following users that follows");
        var notFollowedBack = Followers.Except(Followings).ToList();
        foreach (var user in notFollowedBack)
        {
            bool success = await _gitHubApiService.FollowUserAsync(user);
            if (success)
            {
                Console.WriteLine($"Followed: {user}");
                Followings.Add(user);
            }
            else
            {
                Console.WriteLine($"Unable to Follow: {user}");
            }
        }
        Console.WriteLine($"Followers - {Followers.Count}, Followings - {Followings.Count}");
    }

    public async Task<List<string>> ScrapeFollowersOfFollowers()
    {
        Console.WriteLine("Scraping users to follow");
        var potentialFollowers = new HashSet<string>();
        foreach (var follower in Followers)
        {
            var followerFollowers = await _gitHubApiService.GetFollowersAsync(follower);
            foreach (var f in followerFollowers)
            {
                if (!Followings.Contains(f) && !Followers.Contains(f))
                {
                    potentialFollowers.Add(f);
                }
            }
        }
        Console.WriteLine($"Found {potentialFollowers.Count} users to follow");
        return [.. potentialFollowers];
    }

    public async Task<List<string>> ScrapeFollowersOfFollowers(int targetCount)
    {
        Console.WriteLine($"Scraping users to follow, potential followers {targetCount}");
        var potentialFollowers = new HashSet<string>(); // Avoid duplicates
        var visitedUsers = new HashSet<string>(Followers); // Initialize with known followers
        var queue = new Queue<string>(Followers); // Start with existing followers

        // Ensure the target count respects the initial known list size
        foreach (var follower in Followers)
        {
            if (!Followings.Contains(follower))
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
                var currentFollowers = await _gitHubApiService.GetFollowersAsync(currentUser);

                foreach (var follower in currentFollowers)
                {
                    if (!Followings.Contains(follower) && !Followers.Contains(follower) && !potentialFollowers.Contains(follower))
                    {
                        potentialFollowers.Add(follower);

                        // Stop if the target count is reached
                        if (potentialFollowers.Count >= targetCount)
                        {
                            Console.WriteLine($"Found {potentialFollowers.Count} users to follow");
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
                Console.WriteLine($"Error fetching followers for {currentUser}: {ex.Message}");
            }
        }
        Console.WriteLine($"Found {potentialFollowers.Count} users to follow");
        return potentialFollowers.ToList();
    }

    public async Task FollowPotentialFollowers(List<string> potentialFollowers)
    {
        Console.WriteLine("Following potential followers");
        foreach (var newUser in potentialFollowers)
        {
            bool success = await _gitHubApiService.FollowUserAsync(newUser);
            if (success)
            {
                Console.WriteLine($"Followed: {newUser}");
                Followings.Add(newUser);
            }
            else
            {
                Console.WriteLine($"Unable to Follow: {newUser}");
            }
        }
        await _gitHubApiService.LogRateLimitAsync();
    }

    public async Task FollowPotentialFollowers(List<string> potentialFollowers, int num)
    {
        foreach (var newUser in potentialFollowers.Take(num))
        {
            bool success = await _gitHubApiService.FollowUserAsync(newUser);
            if (success)
            {
                Console.WriteLine($"Followed: {newUser}");
                Followings.Add(newUser);
            }
            else
            {
                Console.WriteLine($"Unable to Follow: {newUser}");
            }
        }

        await _gitHubApiService.LogRateLimitAsync();
    }
}