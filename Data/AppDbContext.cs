using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NguyenQuangHuy_23805614880_API.Models;

namespace NguyenQuangHuy_23805614880_API.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<EmailOtp> EmailOtps { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC07D40B93CC");

            entity.Property(e => e.CheckInMethod)
                .HasMaxLength(30)
                .HasDefaultValue("QR");
            entity.Property(e => e.CheckInTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendances_Employees");
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailOtp__3214EC0776366F97");

            entity.Property(e => e.CodeHash).HasMaxLength(500);
            entity.Property(e => e.Purpose).HasMaxLength(30);

            entity.HasOne(d => d.User).WithMany(p => p.EmailOtps)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailOtps_Users");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC0766B66496");

            entity.HasIndex(e => e.UserId, "UQ__Employee__1788CC4DB5FC4801").IsUnique();

            entity.HasIndex(e => e.EmployeeCode, "UQ__Employee__1F6425482A789E91").IsUnique();

            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.EmployeeCode).HasMaxLength(30);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Position).HasMaxLength(100);

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07716E4F48");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105346EA75851").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("Employee");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
