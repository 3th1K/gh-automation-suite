using Octokit;

namespace Application.Interfaces;

public interface IGitHubContributionService
{
    void Configure(string userApiToken, string repositoryName);

    Task<Repository> EnsureRepositoryExistsAsync();

    Task<string> EnsureFileExistsAsync(Repository repository, string fileName);

    Task CommitChangesAsync(Repository repository, string fileName, string updatedContent);

    string GenerateGibberishText();
}