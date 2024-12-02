using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFollowerManager(this IServiceCollection services, IConfiguration config)
    {
        string token = config["token"] ?? throw new ArgumentNullException();
        string username = config["username"] ?? throw new ArgumentNullException();

        services.AddHttpClient<IGitHubApiService, GitHubApiService>((client) =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubApiSolutions");
        });

        services.AddScoped<IGitHubFollowerManagerService>(provider =>
            new GitHubFollowerManagerService(
                provider.GetRequiredService<IGitHubApiService>(),
                username
            ));

        return services;
    }

    public static IServiceCollection AddFollowerManager(this IServiceCollection services, string username, string token)
    {
        services.AddHttpClient<IGitHubApiService, GitHubApiService>((client) =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubApiSolutions");
        });

        services.AddScoped<IGitHubFollowerManagerService>(provider =>
            new GitHubFollowerManagerService(
                provider.GetRequiredService<IGitHubApiService>(),
                username
            ));

        return services;
    }
}