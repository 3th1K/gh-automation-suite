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
                _gitHubService.Configure(request.Username, request.Token, request.RepositoryName);

                await _gitHubService.AutomateGitHubContributionAsync();
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