using Application.Interfaces;
using Domain.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace GithubAutomationApi.Controllers;

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

    [HttpPost("schedule")]
    public IActionResult ScheduleJob([FromBody] JobRequest request)
    {
        try
        {
            _gitHubService.ScheduleGitHubContribution(request.Token, request.RepositoryName, request.IntervalInMinutes);
            return Ok($"Job scheduled for token {request.Token} immediately and then every {request.IntervalInMinutes} minutes.");
        }
        catch (ArgumentNullException) 
        {
            return BadRequest("Invalid token or interval.");
        }
        catch (ArgumentException)
        {
            return Conflict($"A job for token '{request.Token}' is already scheduled.");
        }
    }


    [HttpPost("unschedule")]
    public IActionResult UnscheduleJob([FromQuery]string token)
    {
        try
        {
            var res = _gitHubService.UnscheduleGitHubContribution(token);
            if(!res)
                return NotFound($"No job found for token '{token}'.");
            
            return Ok($"Job for token {token} has been unscheduled.");
        }
        catch (ArgumentNullException)
        {
            return BadRequest("Invalid token.");
        }
    }

    [HttpGet("scheduled")]
    public IActionResult GetScheduledJobs([FromQuery] string token)
    {
        try
        {
            var res = _gitHubService.CheckScheduled(token);
            return Ok(res);
        }
        catch (ArgumentNullException)
        {
            return BadRequest("Invalid token.");
        }
    }
}

