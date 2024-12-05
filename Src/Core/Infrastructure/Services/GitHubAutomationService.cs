using Application.Interfaces;
using Octokit;
using System.Text;

namespace Infrastructure.Services;

public class GitHubAutomationService : IGitHubAutomationService
{
    private string _userName;
    private string _userApiToken;
    private string _repositoryName;

    private readonly GitHubClient _client;

    public GitHubAutomationService(GitHubClient client)
    {
        _client = client;
    }

    public void Configure(string userName, string userApiToken, string repositoryName)
    {
        _userName = userName;
        _userApiToken = userApiToken;
        _repositoryName = repositoryName;

        _client.Credentials = new Credentials(_userApiToken);
    }

    public async Task AutomateGitHubContributionAsync()
    {
        var repository = await EnsureRepositoryExistsAsync();

        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.txt";
        var fileContent = await EnsureFileExistsAsync(repository, fileName);

        var updatedContent = fileContent + "\n" + GenerateGibberishText();

        await CommitChangesAsync(repository, fileName, updatedContent);

        Console.WriteLine("Automated contribution successfully completed!");
    }

    private async Task<Repository> EnsureRepositoryExistsAsync()
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

    private async Task<string> EnsureFileExistsAsync(Repository repository, string fileName)
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

    private async Task CommitChangesAsync(Repository repository, string fileName, string updatedContent)
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

    private string GenerateGibberishText()
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