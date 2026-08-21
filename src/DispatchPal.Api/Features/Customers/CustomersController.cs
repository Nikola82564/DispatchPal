using DispatchPal.Api.Domain.Entities;
using DispatchPal.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DispatchPal.Api.Common.Pagination;
using Microsoft.AspNetCore.Authorization;

namespace DispatchPal.Api.Features.Customers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(DispatchPalDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {

        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        var emailAlreadyExists = await dbContext.Customers
            .AnyAsync(
        customer => customer.Email == normalizedEmail,
        cancellationToken);

        if (emailAlreadyExists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Customer email already exists.",
                Detail = $"A customer with email '{normalizedEmail}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim()
        };

        dbContext.Customers.Add(customer);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Customers_Email"
            })
        {
            return Conflict(new ProblemDetails
            {
                Title = "Customer email already exists.",
                Detail = $"A customer with email '{normalizedEmail}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var response = MapToResponse(customer);

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(
            customer => customer.Id == id,
            cancellationToken);

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(customer));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> GetAll(
    [FromQuery] GetCustomersQuery request,
    CancellationToken cancellationToken)
    {
        var customersQuery = dbContext.Customers
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            customersQuery = customersQuery.Where(customer =>
                EF.Functions.ILike(
                    customer.Name,
                    $"%{search}%") ||
                EF.Functions.ILike(
                    customer.Email,
                    $"%{search}%"));
        }

            var totalCount = await customersQuery.CountAsync(cancellationToken);

            var customers = await customersQuery
                .OrderBy(customer => customer.Name)
                .ThenBy(customer => customer.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(customer => new CustomerResponse(
                    customer.Id,
                    customer.Name,
                    customer.Email,
                    customer.Phone,
                    customer.CreatedAtUtc))
                .ToListAsync(cancellationToken);

            return Ok(
        new PagedResponse<CustomerResponse>(
            customers,
            request.Page,
            request.PageSize,
            totalCount));
        }

    private static CustomerResponse MapToResponse(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.CreatedAtUtc);
    }
}