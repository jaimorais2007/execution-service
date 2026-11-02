using ExecutionService.Application.Abstractions;
using ExecutionService.Application.Messaging;
using ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Infrastructure.Data;

public class ExecutionDbContext(DbContextOptions<ExecutionDbContext> options)
    : DbContext(options), IExecutionDbContext
{
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ExecutionOrder> ExecutionOrders => Set<ExecutionOrder>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Part>(builder =>
        {
            builder.ToTable("parts");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Price).HasPrecision(18, 2);
            builder.Property(p => p.QuantityInStock).IsRequired();
            builder.Property(p => p.QuantityReserved).IsRequired();
            builder.Property(p => p.Inactive).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.Version).IsConcurrencyToken();

            builder.HasIndex(p => p.Code).IsUnique();
        });

        modelBuilder.Entity<Service>(builder =>
        {
            builder.ToTable("services");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
            builder.Property(s => s.Description).HasMaxLength(500);
            builder.Property(s => s.DefaultPrice).HasPrecision(18, 2);
            builder.Property(s => s.Inactive).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<ExecutionOrder>(builder =>
        {
            builder.ToTable("execution_orders");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.ServiceOrderId).IsRequired();
            builder.Property(e => e.Status).IsRequired();
            builder.Property(e => e.QueuedAt).IsRequired();
            builder.Property(e => e.DiagnosisNotes).HasMaxLength(1000);
            builder.Property(e => e.CompletionNotes).HasMaxLength(1000);
            builder.Property(e => e.FailureReason).HasMaxLength(ExecutionOrder.MaxReasonLength);

            builder.HasIndex(e => e.ServiceOrderId);

            builder.OwnsMany(e => e.Parts, partBuilder =>
            {
                partBuilder.ToTable("execution_order_parts");
                partBuilder.HasKey(p => p.Id);
                partBuilder.WithOwner().HasForeignKey(p => p.ExecutionOrderId);
                partBuilder.Property(p => p.PartName).IsRequired().HasMaxLength(150);
                partBuilder.Property(p => p.Quantity).IsRequired();
            });

            builder.OwnsMany(e => e.Services, serviceBuilder =>
            {
                serviceBuilder.ToTable("execution_order_services");
                serviceBuilder.HasKey(s => s.Id);
                serviceBuilder.WithOwner().HasForeignKey(s => s.ExecutionOrderId);
                serviceBuilder.Property(s => s.ServiceName).IsRequired().HasMaxLength(150);
            });
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.EventType).IsRequired().HasMaxLength(100);
            builder.Property(m => m.CorrelationId).IsRequired();
            builder.Property(m => m.Content).IsRequired();
            builder.Property(m => m.TraceParent).HasMaxLength(200);
            builder.Property(m => m.OccurredAt).IsRequired();
            builder.Property(m => m.LastError).HasMaxLength(OutboxMessage.MaxErrorLength);

            builder.HasIndex(m => m.PublishedAt);
        });

        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.ToTable("processed_messages");
            builder.HasKey(m => m.EventId);
            builder.Property(m => m.EventType).IsRequired().HasMaxLength(100);
            builder.Property(m => m.ProcessedAt).IsRequired();
        });
    }
}
