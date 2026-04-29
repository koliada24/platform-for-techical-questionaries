using Api.Contracts;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.InProgress;

public class TestsInProgressService : ITestsInProgressService
{
    private readonly AppDbContext _db;

    public TestsInProgressService(AppDbContext db)
    {
        _db = db;
    }
}
