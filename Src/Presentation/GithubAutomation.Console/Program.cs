using Application.Interfaces;
using Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up the service collection
        var services = new ServiceCollection();

        // Register services
        ConfigureServices(services);

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Resolve the automation service
        var automationService = serviceProvider.GetRequiredService<IGitHubAutomationService>();

        // Run the main menu
        await RunMainMenuAsync(automationService);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(configure => configure.AddConsole());
        services.AddGithubAutomationService();
    }

    private static async Task RunMainMenuAsync(IGitHubAutomationService automationService)
    {
        bool running = true;
        while (running)
        {
            Console.Clear();
            DisplayMainMenu();

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunGithubSocialAutomationAsync(automationService);
                    break;
                case "2":
                    // Continue the loop to go back to the main menu
                    break;
                case "3":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static void DisplayMainMenu()
    {
        Console.WriteLine("Menu");
        Console.WriteLine("1. Github Social Automation");
        Console.WriteLine("2. Back");
        Console.WriteLine("3. Quit");
        Console.Write("Enter your choice: ");
    }

    private static async Task RunGithubSocialAutomationAsync(IGitHubAutomationService service)
    {
        // Get user inputs
        int numberOfPeopleToFollow = GetNumberOfPeopleToFollow();
        bool unfollowNotFollowingBack = GetUserConfirmation("Do Unfollow Users who are Not Following Back? y/n: ");
        bool followNotFollowedBack = GetUserConfirmation("Do Follow Users whom you did Not Followed Back? y/n: ");
        string userToken = GetUserToken();

        // Display selected options
        DisplaySelectedOptions(numberOfPeopleToFollow, unfollowNotFollowingBack, followNotFollowedBack);

        // Start automation
        Console.WriteLine("Starting automation...");
        await service.AutomateGitHubSocialBoostAsync(userToken, numberOfPeopleToFollow, unfollowNotFollowingBack, followNotFollowedBack);

        Console.WriteLine("\nPress any key to return to the main menu...");
        Console.ReadKey();
    }

    private static int GetNumberOfPeopleToFollow()
    {
        Console.Write("Enter number of people to follow (default 100): ");
        string input = Console.ReadLine();
        return int.TryParse(input, out int number) ? number : 100;
    }

    private static bool GetUserConfirmation(string prompt)
    {
        Console.Write(prompt);
        string input = Console.ReadLine();
        return input?.ToLower() == "y";
    }

    private static string GetUserToken()
    {
        Console.Write("Enter your GitHub API token: ");
        return Console.ReadLine() ?? throw new InvalidOperationException("User token is required.");
    }

    private static void DisplaySelectedOptions(int numberOfPeopleToFollow, bool unfollowNotFollowingBack, bool followNotFollowedBack)
    {
        Console.WriteLine("\nSelected Options:");
        Console.WriteLine($"Number of people to follow: {numberOfPeopleToFollow}");
        Console.WriteLine($"Unfollow Users who are Not Following Back: {unfollowNotFollowingBack}");
        Console.WriteLine($"Follow Users whom you did Not Followed Back: {followNotFollowedBack}");
    }
}