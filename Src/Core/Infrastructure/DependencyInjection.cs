using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Octokit;

namespace Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialService(this IServiceCollection services)
    {
        services.AddScoped<IGitHubSocialService, GitHubSocialService>();
        return services;
    }

    public static IServiceCollection AddGithubAutomationService(this IServiceCollection services)
    {
        services.AddScoped(provider =>
        {
            return new GitHubClient(new ProductHeaderValue("GitHubAutomationApp"));
        });
        services.AddScoped<IGitHubSocialService, GitHubSocialService>();
        services.AddScoped<IGitHubContributionService, GitHubContributionService>();
        services.AddScoped<IGitHubAutomationService, GitHubAutomationService>();

        return services;
    }
}