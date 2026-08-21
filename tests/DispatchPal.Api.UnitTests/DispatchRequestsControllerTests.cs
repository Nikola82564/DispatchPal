using DispatchPal.Api.Domain.Entities;
using DispatchPal.Api.Features.DispatchRequests;
using DispatchPal.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DispatchPal.Api.UnitTests;

public sealed class DispatchRequestsControllerTests
{
    [Fact]
    public async Task Create_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        var options =
            new DbContextOptionsBuilder<DispatchPalDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var dbContext = new DispatchPalDbContext(options);

        var controller = new DispatchRequestsController(dbContext);

        var request = new CreateDispatchRequestRequest(
            CustomerId: Guid.NewGuid(),
            PickupAddress: "Beograd",
            DeliveryAddress: "Novi Sad",
            PackageDescription: "Laptop");

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result.Result);

        var problemDetails =
            Assert.IsType<ProblemDetails>(notFoundResult.Value);

        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("Customer not found.", problemDetails.Title);
        Assert.Empty(dbContext.DispatchRequests);
        Assert.Empty(dbContext.OutboxMessages);
    }

    [Fact]
    public async Task Create_WhenCustomerExists_CreatesRequestHistoryAndOutboxMessage()
    {
        var options =
            new DbContextOptionsBuilder<DispatchPalDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var dbContext = new DispatchPalDbContext(options);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Marko Markovic",
            Email = "marko@example.com",
            Phone = "0601234567"
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var controller = new DispatchRequestsController(dbContext);

        var request = new CreateDispatchRequestRequest(
        CustomerId: customer.Id,
        PickupAddress: "  Beograd  ",
        DeliveryAddress: "  Novi Sad  ",
        PackageDescription: "  Laptop  ");

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

        var response =
        Assert.IsType<DispatchRequestResponse>(createdResult.Value);

        Assert.Equal(nameof(DispatchRequestsController.GetById),
        createdResult.ActionName);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal("Beograd", response.PickupAddress);
        Assert.Equal("Novi Sad", response.DeliveryAddress);
        Assert.Equal("Laptop", response.PackageDescription);
        Assert.Equal(DispatchRequestStatus.Pending, response.Status);

        var savedRequest =
            await dbContext.DispatchRequests
                  .Include(x => x.StatusHistory)
                  .SingleAsync();

        Assert.Equal(savedRequest.Id, response.Id);

        var history = Assert.Single(savedRequest.StatusHistory);

        Assert.Equal(DispatchRequestStatus.Pending, history.Status);
        Assert.Equal("Dispatch request created.", history.Description);

        var outboxMessage =
            await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal("DispatchRequestCreated", outboxMessage.EventType);
        Assert.Equal("dispatch-request.created", outboxMessage.RoutingKey);
        Assert.Null(outboxMessage.PublishedAtUtc);
        Assert.Contains(response.Id.ToString(), outboxMessage.Payload);
    }

    [Fact]
    public async Task GetById_ReturnsStatusHistoryOrderedByChangedAtUtc()
    {
        var options =
     new DbContextOptionsBuilder<DispatchPalDbContext>()
         .UseInMemoryDatabase(Guid.NewGuid().ToString())
         .Options;

        await using var dbContext = new DispatchPalDbContext(options);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Marko Markovic",
            Email = "marko-history@example.com",
            Phone = "0601234567"
        };

        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var dispatchRequest = new DispatchRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            PickupAddress = "Beograd",
            DeliveryAddress = "Novi Sad",
            PackageDescription = "Laptop",
            Status = DispatchRequestStatus.Completed,
            CreatedAtUtc = createdAt
        };

        dispatchRequest.StatusHistory.Add(
            new DispatchRequestStatusHistory
            {
                Id = Guid.NewGuid(),
                DispatchRequestId = dispatchRequest.Id,
                Status = DispatchRequestStatus.Completed,
                Description = "Dispatch request processed successfully.",
                ChangedAtUtc = createdAt.AddMinutes(2)
            });

        dispatchRequest.StatusHistory.Add(
            new DispatchRequestStatusHistory
            {
                Id = Guid.NewGuid(),
                DispatchRequestId = dispatchRequest.Id,
                Status = DispatchRequestStatus.Pending,
                Description = "Dispatch request created.",
                ChangedAtUtc = createdAt
            });

        dbContext.DispatchRequests.Add(dispatchRequest);
        await dbContext.SaveChangesAsync();

        var controller = new DispatchRequestsController(dbContext);

        var result = await controller.GetById(
            dispatchRequest.Id,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
        Assert.IsType<DispatchRequestResponse>(okResult.Value);

        Assert.Equal(DispatchRequestStatus.Completed, response.Status);
        Assert.Equal(2, response.StatusHistory.Count);

        Assert.Equal(DispatchRequestStatus.Completed, response.Status);
        Assert.Equal(2, response.StatusHistory.Count);

        Assert.Equal(
            DispatchRequestStatus.Pending,
            response.StatusHistory[0].Status);

        Assert.Equal(
            DispatchRequestStatus.Completed,
            response.StatusHistory[1].Status);

        Assert.True(
            response.StatusHistory[0].ChangedAtUtc
            < response.StatusHistory[1].ChangedAtUtc);
    }
}