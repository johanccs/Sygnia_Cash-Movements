using Microsoft.EntityFrameworkCore;
using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Entities;

namespace Sygnia.Infrastructure.Persistence;

public sealed class SygniaDbContext(DbContextOptions<SygniaDbContext> options) : DbContext(options)
{
    public DbSet<MovementEntity> Movements => Set<MovementEntity>();

    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(a => a.AccountId);
            entity.Property(a => a.AccountId).HasMaxLength(Account.AccountIdMaxLength);
            entity.Property(a => a.AccountName).HasMaxLength(Account.AccountNameMaxLength).IsRequired();
            entity.Property(a => a.ContactPerson).HasMaxLength(Account.ContactPersonMaxLength);
            entity.Property(a => a.CreatedBy).HasMaxLength(Account.CreatedByMaxLength).IsRequired();
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasMaxLength(User.IdMaxLength);
            entity.Property(u => u.Name).HasMaxLength(User.NameMaxLength).IsRequired();
            entity.Property(u => u.Surname).HasMaxLength(User.SurnameMaxLength).IsRequired();
        });

        modelBuilder.Entity<MovementEntity>(entity =>
        {
            entity.ToTable("Movements");

            // The composite key IS the idempotency mechanism — see IMovementRepository.
            entity.HasKey(m => new { m.AccountId, m.ExternalRef });

            entity.Property(m => m.AccountId).HasMaxLength(Movement.AccountIdMaxLength);
            entity.Property(m => m.ExternalRef).HasMaxLength(Movement.ExternalRefMaxLength);
            entity.Property(m => m.Currency).HasMaxLength(3).IsRequired();
            entity.Property(m => m.Amount).HasColumnType("DECIMAL(19,4)");
            entity.Property(m => m.Narration).HasMaxLength(Movement.NarrationMaxLength);
            entity.Property(m => m.MovedBy).HasMaxLength(Movement.MovedByMaxLength).IsRequired();

            entity.HasOne(m => m.Account)
                .WithMany(a => a.Movements)
                .HasForeignKey(m => m.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Serves both the balance SUM and the statement scan from the index alone.
            entity.HasIndex(m => new { m.AccountId, m.OccurredAt })
                .IncludeProperties(m => m.Amount)
                .HasDatabaseName("IX_Movements_Account_OccurredAt");
        });
    }
}
