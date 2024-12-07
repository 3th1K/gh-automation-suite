using Application.Interfaces;
using Domain.Requests;
using Microsoft.AspNetCore.Mvc;

namespace GithubAutomationApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GithubAutomationController : ControllerBase
    {
        private readonly IGitHubAutomationService _gitHubService;

        public GithubAutomationController(IGitHubAutomationService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        [HttpPost]
        public async Task<IActionResult> TriggerContribution([FromBody] TriggerContributionRequest request)
        {
            try
            {
                await _gitHubService.AutomateGitHubContributionAsync(request.Token, request.RepositoryName);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(ex.ToString());
            }
        }
    }
}