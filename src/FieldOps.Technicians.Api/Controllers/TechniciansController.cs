using FieldOps.Technicians.Api.Data;
using FieldOps.Technicians.Api.DTOs;
using FieldOps.Technicians.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

    [Authorize(Roles = "Dispatcher,Administrator")]
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

    [Authorize(Roles = "Dispatcher,Administrator")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TechnicianResponse>> Update(Guid id, UpdateTechnicianRequest request, CancellationToken cancellationToken)
    {
        var technician = await _dbContext.Technicians.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (technician is null)
        {
            return NotFound(new
            {
                message = $"Technician '{id}' was not found."
            });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Technicians.AnyAsync(x => x.Id != id && x.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "Another technician already uses this email."
            });
        }

        technician.FirstName = request.FirstName.Trim();
        technician.LastName = request.LastName.Trim();
        technician.Email = normalizedEmail;
        technician.PhoneNumber = request.PhoneNumber?.Trim();
        technician.Skills = request.Skills?.Trim();
        technician.IsActive = request.IsActive;
        technician.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Technician {TechnicianId} was updated.", technician.Id);

        return Ok(MapToResponse(technician));
    }

    [Authorize(Roles = "Dispatcher,Administrator")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var technician = await _dbContext.Technicians.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (technician is null)
        {
            return NotFound(new
            {
                message = $"Technician '{id}' was not found."
            });
        }

        // DELETE is idempotent: an already inactive technician
        // remains inactive and still returns success.
        if (technician.IsActive)
        {
            technician.IsActive = false;
            technician.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Technician {TechnicianId} was deactivated.", technician.Id);
        }

        return NoContent();
    }


    private static TechnicianResponse MapToResponse(Technician technician)
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