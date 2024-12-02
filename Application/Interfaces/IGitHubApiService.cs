using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IGitHubApiService
{
    Task<List<string>> GetFollowersAsync(string username);

    Task<List<string>> GetFollowingAsync(string username);

    Task<bool> FollowUserAsync(string username);

    Task<bool> UnfollowUserAsync(string username);

    Task CheckRateLimitAsync();

    Task LogRateLimitAsync();
}