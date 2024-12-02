using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IGitHubFollowerManagerService
{
    Task FetchFollowersAndFollowing();

    Task UnfollowUsersNotFollowingBack();

    Task FollowUsersNotFollowedBack();

    Task<List<string>> ScrapeFollowersOfFollowers();

    Task<List<string>> ScrapeFollowersOfFollowers(int targetCount);

    Task FollowPotentialFollowers(List<string> potentialFollowers);

    Task FollowPotentialFollowers(List<string> potentialFollowers, int num);
}