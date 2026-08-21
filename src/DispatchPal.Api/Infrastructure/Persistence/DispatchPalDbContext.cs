using DispatchPal.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchPal.Api.Infrastructure.Persistence;

public sealed class DispatchPalDbContext(DbContextOptions<DispatchPalDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<DispatchRequest> DispatchRequests => Set<DispatchRequest>();

    public DbSet<DispatchRequestStatusHistory> DispatchRequestStatusHistories =>
    Set<DispatchRequestStatusHistory>();

    public DbSet<InboxMessage> InboxMessages =>
    Set<InboxMessage>();

    public DbSet<OutboxMessage> OutboxMessages =>
    Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<Customer>();

        modelBuilder.HasPostgresExtension("citext");

        customer.Property(x => x.Name)
            .HasMaxLength(200);

        customer.Property(x => x.Email)
            .HasMaxLength(320);

        customer.Property(x => x.Phone)
            .HasMaxLength(30);

        customer.HasIndex(x => x.Email)
            .IsUnique();

        customer.Property(x => x.Email)
           .HasColumnType("citext")
           .HasMaxLength(320);

        var dispatchRequest = modelBuilder.Entity<DispatchRequest>();

        dispatchRequest.Property(x => x.PickupAddress)
             .HasMaxLength(500);

        dispatchRequest.Property(x => x.DeliveryAddress)
            .HasMaxLength(500);

        dispatchRequest.Property(x => x.PackageDescription)
            .HasMaxLength(1_000);

        dispatchRequest
            .HasOne(x => x.Customer)
            .WithMany(x => x.DispatchRequests)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        var dispatchRequestStatusHistory = modelBuilder.Entity<DispatchRequestStatusHistory>();

        dispatchRequestStatusHistory.HasKey(history => history.Id);

        dispatchRequestStatusHistory.Property(history => history.Description)
            .HasMaxLength(500)
            .IsRequired();

        dispatchRequestStatusHistory.HasOne(history => history.DispatchRequest)
            .WithMany(request => request.StatusHistory)
            .HasForeignKey(history => history.DispatchRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        dispatchRequestStatusHistory.HasIndex(history => new
        {
            history.DispatchRequestId,
            history.ChangedAtUtc
        });

        var inboxMessage = modelBuilder.Entity<InboxMessage>();

        inboxMessage.HasKey(message => message.EventId);

        inboxMessage.Property(message => message.EventType)
            .HasMaxLength(200)
            .IsRequired();

        var outboxMessage = modelBuilder.Entity<OutboxMessage>();

        outboxMessage.HasKey(message => message.Id);

        outboxMessage.Property(message => message.EventType)
            .HasMaxLength(200)
            .IsRequired();

        outboxMessage.Property(message => message.RoutingKey)
            .HasMaxLength(200)
            .IsRequired();

        outboxMessage.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        outboxMessage.Property(message => message.LastError)
            .HasMaxLength(2_000);

        outboxMessage.HasIndex(message => new
        {
            message.PublishedAtUtc,
            message.OccurredAtUtc
        });
    }
}
