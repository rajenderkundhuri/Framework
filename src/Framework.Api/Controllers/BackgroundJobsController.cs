using Framework.Application.BackgroundJobs;
using Framework.Application.Identity;
using Framework.Domain.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Background job management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class BackgroundJobsController : ApiControllerBase
{
    private readonly IJobService _jobService;

    public BackgroundJobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    /// Get jobs with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(JobQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetJobs([FromQuery] JobFilter filter)
    {
        if (!HasPermission(Permissions.BackgroundJobsView))
            return Forbid();

        var result = _jobService.GetJobs(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get job by ID
    /// </summary>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(JobInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetJob(string jobId)
    {
        if (!HasPermission(Permissions.BackgroundJobsView))
            return Forbid();

        var job = _jobService.GetJob(jobId);
        if (job == null)
            return NotFound();

        return Ok(job);
    }

    /// <summary>
    /// Get job statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(JobStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetStatistics()
    {
        if (!HasPermission(Permissions.BackgroundJobsView))
            return Forbid();

        var stats = _jobService.GetStatistics();
        return Ok(stats);
    }

    /// <summary>
    /// Requeue a failed job
    /// </summary>
    [HttpPost("{jobId}/requeue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult RequeueJob(string jobId)
    {
        if (!HasPermission(Permissions.BackgroundJobsManage))
            return Forbid();

        var success = _jobService.Requeue(jobId);
        if (!success)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Trigger a recurring job to run immediately
    /// </summary>
    [HttpPost("recurring/{jobId}/trigger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult TriggerRecurringJob(string jobId)
    {
        if (!HasPermission(Permissions.BackgroundJobsManage))
            return Forbid();

        _jobService.TriggerRecurring(jobId);
        return Ok();
    }

    /// <summary>
    /// Delete a job
    /// </summary>
    [HttpDelete("{jobId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteJob(string jobId)
    {
        if (!HasPermission(Permissions.BackgroundJobsDelete))
            return Forbid();

        var success = _jobService.Delete(jobId);
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Remove a recurring job
    /// </summary>
    [HttpDelete("recurring/{jobId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult RemoveRecurringJob(string jobId)
    {
        if (!HasPermission(Permissions.BackgroundJobsDelete))
            return Forbid();

        _jobService.RemoveRecurring(jobId);
        return NoContent();
    }
}
