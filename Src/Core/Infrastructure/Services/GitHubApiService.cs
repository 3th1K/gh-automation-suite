using Application.Interfaces;
using Domain.Models;
using Newtonsoft.Json;

namespace Infrastructure.Services;

public class GitHubApiService : IGitHubApiService
{
    private readonly HttpClient _httpClient;

    public GitHubApiService(HttpClient client)
    {
        _httpClient = client;
    }

    public async Task<bool> FollowUserAsync(string username)
    {
        var response = await _httpClient.PutAsync($"user/following/{username}", null);
        return (response.IsSuccessStatusCode);
    }

    public async Task<List<string>> GetFollowersAsync(string username)
    {
        var response = await _httpClient.GetAsync($"users/{username}/followers");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var followers = JsonConvert.DeserializeObject<List<User>>(json) ?? [];
        return followers.Select(f => f.Login).ToList();
    }

    public async Task<List<string>> GetFollowingAsync(string username)
    {
        var response = await _httpClient.GetAsync($"users/{username}/following");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var following = JsonConvert.DeserializeObject<List<User>>(json) ?? [];
        return following.Select(f => f.Login).ToList();
    }

    public async Task<bool> UnfollowUserAsync(string username)
    {
        var response = await _httpClient.DeleteAsync($"user/following/{username}");
        return (response.IsSuccessStatusCode);
    }

    public async Task CheckRateLimitAsync()
    {
        var response = await _httpClient.GetAsync("rate_limit");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            dynamic rateLimit = JsonConvert.DeserializeObject(json);

            Console.WriteLine($"Remaining Requests: {rateLimit.rate.remaining}");
            Console.WriteLine($"Reset Time: {DateTimeOffset.FromUnixTimeSeconds((long)rateLimit.rate.reset):u}");
        }
        else
        {
            Console.WriteLine("Failed to fetch rate limit information.");
        }
    }

    public async Task LogRateLimitAsync()
    {
        try
        {
            await CheckRateLimitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking rate limit: {ex.Message}");
        }
    }
}