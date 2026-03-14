using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailPricing.Domain.Entities;

namespace RetailPricing.Infrastructure.Persistence.Configurations;

public class PricingRecordConfiguration : IEntityTypeConfiguration<PricingRecord>
{
    public void Configure(EntityTypeBuilder<PricingRecord> builder)
    {
        builder.ToTable("PricingRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.StoreId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.EffectiveDate)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256);

        // Composite unique constraint: one price per store/sku/date
        builder.HasIndex(x => new { x.StoreId, x.Sku, x.EffectiveDate })
            .IsUnique()
            .HasDatabaseName("IX_PricingRecords_StoreId_Sku_EffectiveDate");

        // Performance indexes for common searches
        builder.HasIndex(x => new { x.StoreId, x.EffectiveDate })
            .HasDatabaseName("IX_PricingRecords_StoreId_EffectiveDate");

        builder.HasIndex(x => x.Sku)
            .HasDatabaseName("IX_PricingRecords_Sku");

        builder.HasIndex(x => new { x.EffectiveDate, x.Price })
            .HasDatabaseName("IX_PricingRecords_EffectiveDate_Price");

        builder.HasOne(x => x.UploadBatch)
            .WithMany(b => b.PricingRecords)
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
