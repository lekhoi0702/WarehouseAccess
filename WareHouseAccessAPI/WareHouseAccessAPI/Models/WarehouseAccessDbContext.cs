using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WarehouseAccessAPI.Models;

public partial class WarehouseAccessDbContext : DbContext
{
    public WarehouseAccessDbContext()
    {
    }

    public WarehouseAccessDbContext(DbContextOptions<WarehouseAccessDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessLog> AccessLogs { get; set; }

    public virtual DbSet<ContactDept> ContactDepts { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Purpose> Purposes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserType> UserTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.0.179;Database=WarehouseAccessDB;User Id=sa;Password=Jiahsin@123;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AccessLo__5E5486480E5ABBE8");

            entity.Property(e => e.LogId).ValueGeneratedNever();
            entity.Property(e => e.CheckInTime).HasColumnType("datetime");
            entity.Property(e => e.CheckOutTime).HasColumnType("datetime");
            entity.Property(e => e.ContactDept).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.EventTime).HasColumnType("datetime");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Purpose).HasMaxLength(255);
            entity.Property(e => e.RecordStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserCode).HasMaxLength(10);
        });

        modelBuilder.Entity<ContactDept>(entity =>
        {
            entity.HasKey(e => e.ContactDeptId).HasName("PK__ContactD__4A07006D032A4CF7");

            entity.Property(e => e.ContactDeptId).ValueGeneratedNever();
            entity.Property(e => e.ContactDeptName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RecordStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DeptName).HasMaxLength(50);
            entity.Property(e => e.RecordStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Purpose>(entity =>
        {
            entity.HasKey(e => e.PurposeId).HasName("PK__Purposes__79E6A1949358D22B");

            entity.Property(e => e.PurposeId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PurposeName).HasMaxLength(255);
            entity.Property(e => e.RecordStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserCode).HasName("PK__Users__1DF52D0D28CEABE6");

            entity.Property(e => e.UserCode).HasMaxLength(10);
            entity.Property(e => e.CardNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.RecordStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UserTypeId)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<UserType>(entity =>
        {
            entity.HasKey(e => e.UserTypeId).HasName("PK__UserType__40D2D8163E82C9E1");

            entity.Property(e => e.UserTypeId)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RecordStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserTypeName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
