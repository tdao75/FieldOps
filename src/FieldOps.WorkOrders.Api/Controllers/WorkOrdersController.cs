using FieldOps.WorkOrders.Api.Data;
using FieldOps.WorkOrders.Api.DTOs;
using FieldOps.WorkOrders.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FieldOps.WorkOrders.Api.Enums;
using FieldOps.Contracts.Events;
using System.Text.Json;
using FieldOps.WorkOrders.Api.Clients;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FieldOps.WorkOrders.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkOrdersController : ControllerBase
    {
        private readonly WorkOrdersDbContext _dbContext;
        private readonly ILogger<WorkOrdersController> _logger;

        private readonly TechniciansClient _techniciansClient;

        public WorkOrdersController(WorkOrdersDbContext context, ILogger<WorkOrdersController> logger, TechniciansClient techniciansClient)
        {
            _dbContext = context;
            _logger = logger;
            _techniciansClient = techniciansClient;
        }

        [HttpGet]
        public async Task<ActionResult<PagedWorkOrdersResponse>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] WorkOrderStatus? status = null,
            [FromQuery] WorkOrderPriority? priority = null,
            CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var baseQuery = _dbContext.WorkOrders.AsNoTracking();

            var allCount = await baseQuery.CountAsync(cancellationToken);

            var openCount = await baseQuery.CountAsync(
                x => x.Status != WorkOrderStatus.Completed &&
                     x.Status != WorkOrderStatus.Cancelled,
                cancellationToken);

            var emergencyCount = await baseQuery.CountAsync(
                x => x.Priority == WorkOrderPriority.Emergency &&
                     x.Status != WorkOrderStatus.Completed &&
                     x.Status != WorkOrderStatus.Cancelled,
                cancellationToken);

            var filteredQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLowerInvariant();

                filteredQuery = filteredQuery.Where(x => x.Title.ToLower().Contains(searchTerm) || x.Location.ToLower().Contains(searchTerm) ||(x.Description != null && x.Description.ToLower().Contains(searchTerm)));
            }

            if (status.HasValue)
            {
                filteredQuery = filteredQuery.Where(x => x.Status == status.Value);
            }

            if (priority.HasValue)
            {
                filteredQuery = filteredQuery.Where(x => x.Priority == priority.Value);
            }

            var totalCount = await filteredQuery.CountAsync(cancellationToken);

            var totalPages = totalCount == 0 ? 0: (int)Math.Ceiling(totalCount / (double)pageSize);

            // Prevent requesting a page beyond the filtered results.
            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var items = await filteredQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => MapToResponse(x))
                .ToListAsync(cancellationToken);

            return Ok(new PagedWorkOrdersResponse
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                AllCount = allCount,
                OpenCount = openCount,
                EmergencyCount = emergencyCount
            });
        }

        [HttpGet("{id:guid}", Name = "GetWorkOrderById")]
        public async Task<ActionResult<WorkOrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var workOrder = await _dbContext.WorkOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (workOrder == null)
            {
                return NotFound(new
                {
                    message = $"Work order '{id}' was not found."
                });
            }

            return Ok(MapToResponse(workOrder));
        }

        [Authorize(Roles = "Dispatcher,Administrator")]
        [HttpPost]
        public async Task<ActionResult<WorkOrderResponse>> Create(CreateWorkOrderRequest request, CancellationToken cancellationToken)
        {
            var workOrder = new WorkOrder
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Location = request.Location.Trim(),
                Priority = request.Priority
            };

            _dbContext.WorkOrders.Add(workOrder);

            _dbContext.WorkOrderAuditEntries.Add(
                CreateAuditEntry(
                    workOrder.Id,
                    WorkOrderAuditAction.Created,
                    previousStatus: null,
                    newStatus: workOrder.Status,
                    occurredAtUtc: workOrder.CreatedAtUtc));

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Work order {WorkOrderId} was created.", workOrder.Id);

            var response = MapToResponse(workOrder);

            return CreatedAtRoute(routeName: "GetWorkOrderById", routeValues: new { id = workOrder.Id }, value: response);
        }

        [Authorize(Roles = "Dispatcher,Administrator")]
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult<WorkOrderResponse>> UpdateStatus(Guid id, UpdateWorkOrderStatusRequest request, CancellationToken cancellationToken)
        {
            var workOrder = await _dbContext.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (workOrder == null)
            {
                return NotFound(new
                {
                    message = $"Work order '{id}' was not found."
                });
            }
            // Repeating the same request is safe.
            if (workOrder.Status == request.Status)
            {
                return Ok(MapToResponse(workOrder));
            }

            if (!IsValidStatusTransition(workOrder.Status, request.Status))
            {
                return Conflict(new
                {
                    message = $"Work order status cannot change from " + $"'{workOrder.Status}' to '{request.Status}'."
                });
            }

            var previousStatus = workOrder.Status;
            var occurredAtUtc = DateTime.UtcNow;

            workOrder.Status = request.Status;
            workOrder.UpdatedAtUtc = DateTime.UtcNow;

            _dbContext.WorkOrderAuditEntries.Add(
                CreateAuditEntry(
                    workOrder.Id,
                    WorkOrderAuditAction.StatusChanged,
                    previousStatus,
                    request.Status,
                    previousTechnicianId:
                        workOrder.AssignedTechnicianId,
                    newTechnicianId:
                        workOrder.AssignedTechnicianId,
                    occurredAtUtc));

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Work order {WorkOrderId} status changed to {Status}.", workOrder.Id, workOrder.Status);

            return Ok(MapToResponse(workOrder));
        }

        [Authorize(Roles = "Dispatcher,Administrator")]
        [HttpPut("{id:guid}/assignment")]
        public async Task<ActionResult<WorkOrderResponse>> Assign(Guid id, AssignWorkOrderRequest request, CancellationToken cancellationToken)
        {
            if (request.TechnicianId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(request.TechnicianId), "A valid technician ID is required.");

                return ValidationProblem(ModelState);
            }

            var workOrder = await _dbContext.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (workOrder is null)
            {
                return NotFound(new
                {
                    message = $"Work order '{id}' was not found."
                });
            }

            // Assignment and reassignment are allowed before work begins.
            if (workOrder.Status != WorkOrderStatus.Submitted && workOrder.Status != WorkOrderStatus.Assigned)
            {
                return Conflict(new
                {
                    message = "Only submitted or assigned work orders can be assigned."
                });
            }

            // Repeating the same request should not change the record.
            if (workOrder.AssignedTechnicianId == request.TechnicianId && workOrder.Status == WorkOrderStatus.Assigned)
            {
                return Ok(MapToResponse(workOrder));
            }

            var technician = await _techniciansClient.GetByIdAsync(request.TechnicianId, cancellationToken);

            if (technician is null)
            {
                return BadRequest(new
                {
                    message =
                        $"Technician '{request.TechnicianId}' does not exist."
                });
            }

            if (!technician.IsActive)
            {
                return Conflict(new
                {
                    message =
                        "The selected technician is inactive."
                });
            }


            var occurredAtUtc = DateTime.UtcNow;
            var previousStatus = workOrder.Status;
            var previousTechnicianId = workOrder.AssignedTechnicianId;

            workOrder.AssignedTechnicianId = request.TechnicianId;
            workOrder.Status = WorkOrderStatus.Assigned;
            workOrder.UpdatedAtUtc = occurredAtUtc;

            var assignmentEvent = new WorkOrderAssignedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAtUtc = occurredAtUtc,
                WorkOrderId = workOrder.Id,
                TechnicianId = technician.Id,
                TechnicianName = $"{technician.FirstName} {technician.LastName}".Trim(),
                TechnicianEmail = technician.Email,
                Title = workOrder.Title,
                Location = workOrder.Location
            };

            var outboxMessage = new OutboxMessage
            {
                Id = assignmentEvent.EventId,
                EventType = nameof(WorkOrderAssignedEvent),
                Payload = JsonSerializer.Serialize(assignmentEvent),
                OccurredAtUtc = occurredAtUtc
            };

            var auditAction = previousTechnicianId.HasValue ? WorkOrderAuditAction.Reassigned : WorkOrderAuditAction.Assigned;

            _dbContext.WorkOrderAuditEntries.Add(
                CreateAuditEntry(
                    workOrder.Id,
                    auditAction,
                    previousStatus,
                    WorkOrderStatus.Assigned,
                    previousTechnicianId,
                    request.TechnicianId,
                    occurredAtUtc));

            _dbContext.OutboxMessages.Add(outboxMessage);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Work order {WorkOrderId} assigned to technician {TechnicianId}.", workOrder.Id, request.TechnicianId);

            return Ok(MapToResponse(workOrder));
        }

        [Authorize(Roles ="Dispatcher,Adminstrator")]
        [HttpPut("{id:guid}/details")]
        public async Task<ActionResult<WorkOrderResponse>> UpdateDetails(Guid id, UpdateWorkOrderDetailsRequest request, CancellationToken cancellationToken)
        {
            var workOrder = await _dbContext.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if(workOrder is null)
            {
                return NotFound(new
                {
                    message = $"Work order '{id}' was not found."
                });
            }
            if(workOrder.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
            {
                return Conflict(new
                {
                    message = "Completed or cancelled work orders cannot be edited."
                });
            }

            var title = request.Title.Trim();
            var description = request.Description?.Trim();
            var location = request.Location.Trim();

            // Repeating an identical request should not create
            // another audit record.
            var hasChanges =
                workOrder.Title != title ||
                workOrder.Description != description ||
                workOrder.Location != location ||
                workOrder.Priority != request.Priority;

            if (!hasChanges)
            {
                return Ok(MapToResponse(workOrder));
            }

            var occurredAtUtc = DateTime.UtcNow;

            workOrder.Title = title;
            workOrder.Description = description;
            workOrder.Location = location;
            workOrder.Priority = request.Priority;
            workOrder.UpdatedAtUtc = occurredAtUtc;

            _dbContext.WorkOrderAuditEntries.Add(
                CreateAuditEntry(
                    workOrder.Id,
                    WorkOrderAuditAction.DetailsUpdated,
                    previousStatus: workOrder.Status,
                    newStatus: workOrder.Status,
                    previousTechnicianId:
                        workOrder.AssignedTechnicianId,
                    newTechnicianId:
                        workOrder.AssignedTechnicianId,
                    occurredAtUtc));

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Work order {WorkOrderId} details were updated.", workOrder.Id);

            return Ok(MapToResponse(workOrder));
        }


        private WorkOrderAuditEntry CreateAuditEntry(Guid workOrderId,
                WorkOrderAuditAction action,
                WorkOrderStatus? previousStatus,
                WorkOrderStatus? newStatus,
                Guid? previousTechnicianId = null,
                Guid? newTechnicianId = null,
                DateTime? occurredAtUtc = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);

            return new WorkOrderAuditEntry
            {
                WorkOrderId = workOrderId,
                Action = action,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                PreviousTechnicianId = previousTechnicianId,
                NewTechnicianId = newTechnicianId,
                ChangedByUserId = userId,
                ChangedByEmail = email,
                OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow
            };
        }

        [Authorize(Roles = "Technician,Dispatcher,Administrator")]
        [HttpGet("{id:guid}/history")]
        public async Task<ActionResult<IReadOnlyList<WorkOrderAuditResponse>>> GetHistory(Guid id, CancellationToken cancellationToken)
        {
            var workOrderExist = await _dbContext.WorkOrders.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

            if (!workOrderExist)
            {
                return NotFound(new
                {
                    message = $"Work order '{id}' was not found."
                });
            }

            var history = await _dbContext.WorkOrderAuditEntries.AsNoTracking().Where(x => x.WorkOrderId == id)
                .OrderBy(x => x.OccurredAtUtc)
                .ThenBy(x => x.Id)
                .Select(x => new WorkOrderAuditResponse
                {
                    Id = x.Id,
                    WorkOrderId = x.WorkOrderId,
                    Action = x.Action,
                    PreviousStatus = x.PreviousStatus,
                    NewStatus = x.NewStatus,
                    PreviousTechnicianId =x.PreviousTechnicianId,
                    NewTechnicianId = x.NewTechnicianId,
                    ChangedByUserId =x.ChangedByUserId,
                    ChangedByEmail = x.ChangedByEmail,
                    OccurredAtUtc = x.OccurredAtUtc
                }).ToListAsync(cancellationToken);

            return Ok(history);
        }

        private static WorkOrderResponse MapToResponse(WorkOrder workOrder)
        {
            return new WorkOrderResponse
            {
                Id = workOrder.Id,
                Title = workOrder.Title,
                Description = workOrder.Description,
                Location = workOrder.Location,
                Priority = workOrder.Priority,
                Status = workOrder.Status,
                AssignedTechnicianId =
                workOrder.AssignedTechnicianId,
                CreatedAtUtc = workOrder.CreatedAtUtc,
                UpdatedAtUtc = workOrder.UpdatedAtUtc
            };
        }
        private static bool IsValidStatusTransition(WorkOrderStatus currentStatus, WorkOrderStatus newStatus)
        {
            return currentStatus switch
            {
                WorkOrderStatus.Submitted => newStatus == WorkOrderStatus.Cancelled,

                WorkOrderStatus.Assigned => newStatus is WorkOrderStatus.InProgress or WorkOrderStatus.Cancelled,

                WorkOrderStatus.InProgress => newStatus is WorkOrderStatus.WaitingForParts or WorkOrderStatus.Completed or WorkOrderStatus.Cancelled,

                WorkOrderStatus.WaitingForParts => newStatus is WorkOrderStatus.InProgress or WorkOrderStatus.Completed or WorkOrderStatus.Cancelled,

                WorkOrderStatus.Completed => false,
                WorkOrderStatus.Cancelled => false,

                _ => false
            };
        }
    }
}
