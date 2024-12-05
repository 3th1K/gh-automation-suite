using Application.Interfaces;
using Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

Console.Write("Enter your GitHub username: ");
string? username = Console.ReadLine();
Console.Write("Enter your GitHub API token: ");
string? token = Console.ReadLine();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
    services.AddFollowerManager(config);
else
    services.AddFollowerManager(username, token);

var serviceProvider = services.BuildServiceProvider();

var githubFollowerManagerService = serviceProvider.GetRequiredService<IGitHubFollowerManagerService>();

// Step 1: Fetch followers and following
await githubFollowerManagerService.FetchFollowersAndFollowing();

// Step 2: Find users to unfollow
await githubFollowerManagerService.UnfollowUsersNotFollowingBack();

// Step 3: Find users to follow back
await githubFollowerManagerService.FollowUsersNotFollowedBack();

// Step 4: Scrape followers of followers
List<string> potentialFollowers = [];
Console.Write("Specify a number of potential followers to scrape [Leave blank for normal scraping]: ");
int num = Convert.ToInt32(Console.ReadLine());
if (num > 0)
    potentialFollowers = await githubFollowerManagerService.ScrapeFollowersOfFollowers(num);
else
    potentialFollowers = await githubFollowerManagerService.ScrapeFollowersOfFollowers();

// Step 5: Follow potential followers
await githubFollowerManagerService.FollowPotentialFollowers(potentialFollowers);

Console.WriteLine("Operation completed.");