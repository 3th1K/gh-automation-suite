using Application.Interfaces;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddFollowerManager(config);

var serviceProvider = services.BuildServiceProvider();

var githubFollowerManagerService = serviceProvider.GetRequiredService<IGitHubFollowerManagerService>();

// Step 1: Fetch followers and following
await githubFollowerManagerService.FetchFollowersAndFollowing();

// Step 2: Find users to unfollow
await githubFollowerManagerService.UnfollowUsersNotFollowingBack();

// Step 3: Find users to follow back
await githubFollowerManagerService.FollowUsersNotFollowedBack();

// Step 4: Scrape followers of followers
var potentialFollowers = await githubFollowerManagerService.ScrapeFollowersOfFollowers();

// Step 5: Follow potential followers
await githubFollowerManagerService.FollowPotentialFollowers(potentialFollowers);

Console.WriteLine("Operation completed.");