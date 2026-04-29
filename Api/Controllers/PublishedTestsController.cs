using Api.Services.Published;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/published-tests")]
public class PublishedTestsController : ControllerBase
{
    private readonly IPublishedTestsService _service;

    public PublishedTestsController(IPublishedTestsService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}/info")]
    public async Task<IActionResult> GetInfo(Guid id, CancellationToken ct)
    {
        var info = await _service.GetInfoAsync(id, ct);
        return info is null ? NotFound() : Ok(info);
    }
}
