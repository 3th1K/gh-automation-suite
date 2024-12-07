using Application.Interfaces;
using Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
services.AddSocialService();

Console.Write("Enter your GitHub API token: ");
string? token = Console.ReadLine();

var serviceProvider = services.BuildServiceProvider();

var socialService = serviceProvider.GetRequiredService<IGitHubSocialService>();

if (string.IsNullOrEmpty(token))
{
    socialService.Configure(config["token"] ?? throw new ArgumentNullException());
}
else
{
    socialService.Configure(token);
}

// Step 1: Fetch followers and following
await socialService.FetchFollowersAndFollowing();

// Step 2: Find users to unfollow
await socialService.UnfollowUsersNotFollowingBack();

// Step 3: Find users to follow back
await socialService.FollowUsersNotFollowedBack();

// Step 4: Scrape followers of followers
List<string> potentialFollowers = [];
Console.Write("Specify a number of potential followers to scrape [Leave blank for normal scraping]: ");
int num = Convert.ToInt32(Console.ReadLine());
if (num > 0)
    potentialFollowers = await socialService.ScrapeFollowersOfFollowers(num);
else
    potentialFollowers = await socialService.ScrapeFollowersOfFollowers();

// Step 5: Follow potential followers
await socialService.FollowPotentialFollowers(potentialFollowers);

Console.WriteLine("Operation completed.");