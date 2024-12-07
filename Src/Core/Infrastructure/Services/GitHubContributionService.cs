using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Text;

namespace Infrastructure.Services;

public class GitHubContributionService : IGitHubContributionService
{
    private string _userName = string.Empty;
    private string _userApiToken = string.Empty;
    private string _repositoryName = string.Empty;

    private readonly ILogger<GitHubContributionService> _logger;
    private readonly GitHubClient _client;

    public GitHubContributionService(ILogger<GitHubContributionService> logger, GitHubClient client)
    {
        _logger = logger;
        _client = client;
    }

    public void Configure(string userApiToken, string repositoryName)
    {
        _userApiToken = userApiToken;
        _client.Credentials = new Credentials(_userApiToken);
        _userName = _client.User.Current().Result.Login;
        _repositoryName = repositoryName;
    }

    public async Task<Repository> EnsureRepositoryExistsAsync()
    {
        try
        {
            return await _client.Repository.Get(_userName, _repositoryName);
        }
        catch (NotFoundException)
        {
            var newRepo = new NewRepository(_repositoryName)
            {
                Private = true
            };
            return await _client.Repository.Create(newRepo);
        }
    }

    public async Task<string> EnsureFileExistsAsync(Repository repository, string fileName)
    {
        try
        {
            var file = await _client.Repository.Content.GetAllContentsByRef(_userName, _repositoryName, fileName, repository.DefaultBranch);
            return file[0].Content;
        }
        catch (NotFoundException)
        {
            var initialContent = GenerateGibberishText();
            await _client.Repository.Content.CreateFile(_userName, _repositoryName, fileName, new CreateFileRequest(
                $"Create {fileName}",
                initialContent,
                repository.DefaultBranch
            ));
            return initialContent;
        }
    }

    public async Task CommitChangesAsync(Repository repository, string fileName, string updatedContent)
    {
        var existingFile = await _client.Repository.Content.GetAllContentsByRef(_userName, _repositoryName, fileName, repository.DefaultBranch);
        var updateRequest = new UpdateFileRequest(
            $"Update {fileName}",
            updatedContent,
            existingFile[0].Sha,
            repository.DefaultBranch
        );

        await _client.Repository.Content.UpdateFile(_userName, _repositoryName, fileName, updateRequest);
    }

    public string GenerateGibberishText()
    {
        var gibberish = new StringBuilder();
        var random = new Random();
        for (int i = 0; i < 1024; i++)
        {
            gibberish.Append((char)random.Next(65, 91));
        }
        return gibberish.ToString();
    }
}