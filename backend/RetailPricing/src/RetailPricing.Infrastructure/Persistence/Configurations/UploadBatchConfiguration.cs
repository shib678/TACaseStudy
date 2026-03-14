using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailPricing.Domain.Entities;

namespace RetailPricing.Infrastructure.Persistence.Configurations;

public class UploadBatchConfiguration : IEntityTypeConfiguration<UploadBatch>
{
    public void Configure(EntityTypeBuilder<UploadBatch> builder)
    {
        builder.ToTable("UploadBatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(x => x.StoreId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ErrorSummary)
            .HasMaxLength(4000);

        builder.Property(x => x.BlobStoragePath)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256);

        builder.HasIndex(x => x.StoreId)
            .HasDatabaseName("IX_UploadBatches_StoreId");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_UploadBatches_CreatedAt");
    }
}
