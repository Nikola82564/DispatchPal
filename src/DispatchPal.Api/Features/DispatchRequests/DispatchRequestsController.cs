using DispatchPal.Api.Common.Pagination;
using DispatchPal.Api.Domain.Entities;
using DispatchPal.Api.Infrastructure.Persistence;
using DispatchPal.Contracts.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace DispatchPal.Api.Features.DispatchRequests;

[ApiController]
[Authorize]
[Route("api/dispatch-requests")]
public sealed class DispatchRequestsController(DispatchPalDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DispatchRequestResponse>> Create(
    CreateDispatchRequestRequest request,
    CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(request.CustomerId),
                "CustomerId must not be empty.");

            return BadRequest(
                new ValidationProblemDetails(ModelState));
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                customer => customer.Id == request.CustomerId,
                cancellationToken);

        if (customer is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found.",
                Detail = $"No customer found with ID '{request.CustomerId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var now = DateTimeOffset.UtcNow;

        var dispatchRequest = new DispatchRequest
        {
            CustomerId = customer.Id,
            PickupAddress = request.PickupAddress.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            PackageDescription = request.PackageDescription.Trim(),
            Status = DispatchRequestStatus.Pending,
            CreatedAtUtc = now
        };

        dispatchRequest.StatusHistory.Add(
            new DispatchRequestStatusHistory
            {
                Id = Guid.NewGuid(),
                DispatchRequestId = dispatchRequest.Id,
                Status = DispatchRequestStatus.Pending,
                Description = "Dispatch request created.",
                ChangedAtUtc = now
            });

        var integrationEvent = new DispatchRequestCreated(
            EventId: Guid.NewGuid(),
            RequestId: dispatchRequest.Id,
            CustomerId: customer.Id,
            CustomerName: customer.Name,
            CustomerEmail: customer.Email,
            PickupAddress: dispatchRequest.PickupAddress,
            DeliveryAddress: dispatchRequest.DeliveryAddress,
            PackageDescription: dispatchRequest.PackageDescription,
            OccurredAtUtc: now);

        var outboxMessage = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            EventType = nameof(DispatchRequestCreated),
            RoutingKey = "dispatch-request.created",
            Payload = JsonSerializer.Serialize(integrationEvent),
            OccurredAtUtc = now,
            PublishedAtUtc = null,
            AttemptCount = 0,
            LastError = null
        };

        dbContext.DispatchRequests.Add(dispatchRequest);
        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(
            dispatchRequest,
            customer.Name);

        return CreatedAtAction(
            nameof(GetById),
            new { id = dispatchRequest.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DispatchRequestResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await dbContext.DispatchRequests
               .AsNoTracking()
               .Where(dispatchRequest => dispatchRequest.Id == id)
               .Select(dispatchRequest => new DispatchRequestResponse(
                   dispatchRequest.Id,
                   dispatchRequest.CustomerId,
                   dispatchRequest.Customer.Name,
                   dispatchRequest.PickupAddress,
                   dispatchRequest.DeliveryAddress,
                   dispatchRequest.PackageDescription,
                   dispatchRequest.Status,
                   dispatchRequest.CreatedAtUtc,
                   dispatchRequest.StatusHistory
                      .OrderBy(history => history.ChangedAtUtc)
                      .Select(history =>
                          new DispatchRequestStatusHistoryResponse(
                              history.Id,
                              history.Status,
                              history.Description,
                              history.ChangedAtUtc))
                      .ToList()))
               .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }
    [HttpGet]
    public async Task<
    ActionResult<PagedResponse<DispatchRequestListItemResponse>>>
    GetAll(
        [FromQuery] GetDispatchRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var dispatchRequestsQuery = dbContext.DispatchRequests
            .AsNoTracking()
            .AsQueryable();

        if (request.CustomerId.HasValue)
        {
            dispatchRequestsQuery = dispatchRequestsQuery.Where(
                dispatchRequest =>
                    dispatchRequest.CustomerId ==
                    request.CustomerId.Value);
        }

        if (request.Status.HasValue)
        {
            dispatchRequestsQuery = dispatchRequestsQuery.Where(
                dispatchRequest =>
                    dispatchRequest.Status ==
                    request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            dispatchRequestsQuery = dispatchRequestsQuery.Where(
                dispatchRequest =>
                    EF.Functions.ILike(
                        dispatchRequest.PickupAddress,
                        $"%{search}%") ||
                    EF.Functions.ILike(
                        dispatchRequest.DeliveryAddress,
                        $"%{search}%") ||
                    EF.Functions.ILike(
                        dispatchRequest.PackageDescription,
                        $"%{search}%") ||
                    EF.Functions.ILike(
                        dispatchRequest.Customer.Name,
                        $"%{search}%"));
        }

        var totalCount = await dispatchRequestsQuery.CountAsync(
            cancellationToken);

        var dispatchRequests = await dispatchRequestsQuery
            .OrderByDescending(
                dispatchRequest =>
                    dispatchRequest.CreatedAtUtc)
            .ThenBy(
                dispatchRequest =>
                    dispatchRequest.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(dispatchRequest =>
                new DispatchRequestListItemResponse(
                    dispatchRequest.Id,
                    dispatchRequest.CustomerId,
                    dispatchRequest.Customer.Name,
                    dispatchRequest.PickupAddress,
                    dispatchRequest.DeliveryAddress,
                    dispatchRequest.PackageDescription,
                    dispatchRequest.Status,
                    dispatchRequest.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(
            new PagedResponse<DispatchRequestListItemResponse>(
                dispatchRequests,
                request.Page,
                request.PageSize,
                totalCount));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DispatchRequestResponse>> Update(
    Guid id,
    UpdateDispatchRequestRequest request,
    CancellationToken cancellationToken)
    {
        var pickupAddress = request.PickupAddress.Trim();
        var deliveryAddress = request.DeliveryAddress.Trim();
        var packageDescription =
            request.PackageDescription.Trim();

        var affectedRows = await dbContext.DispatchRequests
            .Where(dispatchRequest =>
                dispatchRequest.Id == id &&
                dispatchRequest.Status ==
                    DispatchRequestStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        dispatchRequest =>
                            dispatchRequest.PickupAddress,
                        pickupAddress)
                    .SetProperty(
                        dispatchRequest =>
                            dispatchRequest.DeliveryAddress,
                        deliveryAddress)
                    .SetProperty(
                        dispatchRequest =>
                            dispatchRequest.PackageDescription,
                        packageDescription),
                cancellationToken);

        if (affectedRows == 0)
        {
            var dispatchRequestExists =
                await dbContext.DispatchRequests
                    .AsNoTracking()
                    .AnyAsync(
                        dispatchRequest =>
                            dispatchRequest.Id == id,
                        cancellationToken);

            if (!dispatchRequestExists)
            {
                return NotFound();
            }

            return Conflict(new ProblemDetails
            {
                Title = "Dispatch request cannot be edited.",
                Detail =
                    "Only pending dispatch requests can be edited.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var updatedDispatchRequest =
            await dbContext.DispatchRequests
                .AsNoTracking()
                .Include(dispatchRequest =>
                    dispatchRequest.Customer)
                .Include(dispatchRequest =>
                    dispatchRequest.StatusHistory)
                .SingleAsync(
                    dispatchRequest =>
                        dispatchRequest.Id == id,
                    cancellationToken);

        return Ok(
            MapToResponse(
                updatedDispatchRequest,
                updatedDispatchRequest.Customer.Name));
    }

    private static DispatchRequestResponse MapToResponse(
       DispatchRequest dispatchRequest,
       string customerName)
    {
        return new DispatchRequestResponse(
            dispatchRequest.Id,
            dispatchRequest.CustomerId,
            customerName,
            dispatchRequest.PickupAddress,
            dispatchRequest.DeliveryAddress,
            dispatchRequest.PackageDescription,
            dispatchRequest.Status,
            dispatchRequest.CreatedAtUtc,
            dispatchRequest.StatusHistory
                .OrderBy(history => history.ChangedAtUtc)
                .Select(history => new DispatchRequestStatusHistoryResponse(
                    history.Id,
                    history.Status,
                    history.Description,
                    history.ChangedAtUtc))
                .ToList());
    }
}