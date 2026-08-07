using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.Extensions;
using DuonDevKit.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DuonDevKit.EntityFrameworkCore.Tests
{
    /// <summary>Test entity implementing all audit marker interfaces.</summary>
    public class TestEntity : ICanCreate, ICanUpdate, ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    /// <summary>Test entity implementing none of the audit marker interfaces (negative-case fixture).</summary>
    public class PlainEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Test entity using the non-generic <see cref="BaseEntity"/> (string id) for <see cref="Repository{T, TId}"/> tests.</summary>
    public class KeyedTestEntity : BaseEntity, ICanCreate
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    /// <summary>Stub <see cref="ICurrentUserProvider"/> whose <see cref="UserId"/> can be changed mid-test.</summary>
    public class StubCurrentUserProvider : ICurrentUserProvider
    {
        public string? UserId { get; set; }
    }

    /// <summary>TPH hierarchy root, implementing <see cref="ISoftDelete"/> itself — every derived type inherits it.</summary>
    public abstract class Vehicle : ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    public class Car : Vehicle
    {
    }

    public class Truck : Vehicle
    {
    }

    /// <summary>TPH hierarchy root, NOT implementing <see cref="ISoftDelete"/> — only some subtypes opt in.</summary>
    public abstract class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Only this subtype opts into soft-delete.</summary>
    public class Dog : Animal, ISoftDelete
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    /// <summary>Sibling subtype with no soft-delete at all.</summary>
    public class Cat : Animal
    {
    }

    /// <summary>Minimal DbContext used across the test suite, backed by the EF Core InMemory provider.</summary>
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<PlainEntity> PlainEntities => Set<PlainEntity>();
        public DbSet<KeyedTestEntity> KeyedTestEntities => Set<KeyedTestEntity>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Animal> Animals => Set<Animal>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Car>();
            modelBuilder.Entity<Truck>();
            modelBuilder.Entity<Dog>();
            modelBuilder.Entity<Cat>();
            modelBuilder.ApplySoftDeleteQueryFilter();
        }
    }
}
