using System.Security.Claims;
using Api.Contracts;
using Api.Services.InProgress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/tests")]
public class TestsInProgressController : ControllerBase
{
    private readonly ITestsInProgressService _testsService;

    public TestsInProgressController(ITestsInProgressService testsService)
    {
        _testsService = testsService;
    }
}
