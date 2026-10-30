using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransferLab.Domain.Entities;

namespace TransferLab.Infrastructure.Persistence.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedUtc)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(Transfer.MaxIdempotencyKeyLength);

        builder.HasIndex(x => x.SourceAccountId);

        builder.HasIndex(x => x.DestinationAccountId);

        builder.HasIndex(x => new
        {
            x.Status,
            x.CreatedUtc
        });

        builder.HasIndex(x => new
        {
            x.Status,
            x.ProcessingStartedUtc
        });

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
        
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
