using MagdyPOS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductMaterial> ProductMaterials => Set<ProductMaterial>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();
    public DbSet<Return> Returns => Set<Return>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Unit>(entity =>
        {
            entity.ToTable("Units");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Symbol).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
        });

        builder.Entity<Material>(entity =>
        {
            entity.ToTable("Materials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AlertLimit).HasColumnType("REAL").HasDefaultValue(0.0);
            entity.Property(e => e.Quantity).HasColumnType("REAL");
            entity.Property(e => e.OriginalQuantity).HasColumnType("REAL").HasDefaultValue(0.0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Unit)
                .WithMany(u => u.Materials)
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasColumnType("REAL");

            entity.HasOne(e => e.Unit)
                .WithMany(u => u.Products)
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductMaterial>(entity =>
        {
            entity.ToTable("Products_Materials");
            entity.HasKey(e => new { e.ProductId, e.MaterialId });
            entity.Property(e => e.ProductId).HasMaxLength(100);
            entity.Property(e => e.MaterialId).HasMaxLength(100);
            entity.Property(e => e.Quantity).HasColumnType("REAL");

            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductMaterials)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Material)
                .WithMany(m => m.ProductMaterials)
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).HasMaxLength(100);
            entity.Property(e => e.OrderNumber).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.Discount).HasColumnType("REAL");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Tips);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("Order_Details");
            entity.HasKey(e => new { e.OrderId, e.ProductId });
            entity.Property(e => e.OrderId).HasMaxLength(100);
            entity.Property(e => e.ProductId).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("REAL");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("Stock_Movements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.ItemId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(400);
            entity.Property(e => e.ReferenceType).HasMaxLength(100);
            entity.Property(e => e.ReferenceId).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.QuantityDelta).HasColumnType("REAL");
            entity.Property(e => e.QuantityBefore).HasColumnType("REAL");
            entity.Property(e => e.QuantityAfter).HasColumnType("REAL");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ItemType, e.ItemId, e.CreatedAt });
        });

        builder.Entity<Return>(entity =>
        {
            entity.ToTable("Return");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Amount).HasColumnType("REAL");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("REAL");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.MaterialId).HasMaxLength(100);
            entity.Property(e => e.MaterialQuantity).HasColumnType("REAL");

            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.UserName).HasMaxLength(200);

            entity.HasOne(e => e.Material)
                .WithMany()
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.MaterialId);
        });

        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("Attendance_Records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(400);

            entity.Property(e => e.WorkDate).HasColumnType("TEXT");
            entity.Property(e => e.CheckInAtUtc).HasColumnType("TEXT");
            entity.Property(e => e.CheckOutAtUtc).HasColumnType("TEXT");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One record per user per day.
            entity.HasIndex(e => new { e.UserId, e.WorkDate }).IsUnique();
            entity.HasIndex(e => e.WorkDate);
        });

        builder.Entity<OrganizationProfile>(entity =>
        {
            entity.ToTable("Organization_Profile");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500).IsRequired();
        });
    }
}
