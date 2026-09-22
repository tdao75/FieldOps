using FieldOps.Technicians.Api.Data;
using FieldOps.Technicians.Api.DTOs;
using FieldOps.Technicians.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.Technicians.Api.Controllers;

[ApiController]
[Route("api/technicians")]
public sealed class TechniciansController : ControllerBase
{
    private readonly TechniciansDbContext _dbContext;
    private readonly ILogger<TechniciansController> _logger;

    public TechniciansController(TechniciansDbContext dbContext, ILogger<TechniciansController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TechnicianResponse>>> GetAll(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Technicians.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var technicians = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => MapToResponse(x))
            .ToListAsync(cancellationToken);

        return Ok(technicians);
    }

    [HttpGet("{id:guid}", Name = "GetTechnicianById")]
    public async Task<ActionResult<TechnicianResponse>> GetById(Guid id,CancellationToken cancellationToken)
    {
        var technician = await _dbContext.Technicians
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (technician is null)
        {
            return NotFound(new
            {
                message = $"Technician '{id}' was not found."
            });
        }

        return Ok(MapToResponse(technician));
    }

    [HttpPost]
    public async Task<ActionResult<TechnicianResponse>> Create(CreateTechnicianRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail =request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Technicians.AnyAsync(x => x.Email == normalizedEmail,cancellationToken);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "A technician with this email already exists."
            });
        }

        var technician = new Technician
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Skills = request.Skills?.Trim()
        };

        _dbContext.Technicians.Add(technician);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Technician {TechnicianId} was created.", technician.Id);

        var response = MapToResponse(technician);

        return CreatedAtRoute(routeName: "GetTechnicianById", routeValues: new { id = technician.Id }, value: response);
    }

    private static TechnicianResponse MapToResponse(
        Technician technician)
    {
        return new TechnicianResponse
        {
            Id = technician.Id,
            FirstName = technician.FirstName,
            LastName = technician.LastName,
            Email = technician.Email,
            PhoneNumber = technician.PhoneNumber,
            Skills = technician.Skills,
            IsActive = technician.IsActive,
            CreatedAtUtc = technician.CreatedAtUtc,
            UpdatedAtUtc = technician.UpdatedAtUtc
        };
    }
}