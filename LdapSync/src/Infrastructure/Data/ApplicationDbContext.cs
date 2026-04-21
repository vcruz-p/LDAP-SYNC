using Microsoft.EntityFrameworkCore;
using LdapSync.Domain.Entities;

namespace LdapSync.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<LdapServer> LdapServers { get; set; }
    public DbSet<LdapUser> LdapUsers { get; set; }
    public DbSet<LdapGroup> LdapGroups { get; set; }
    public DbSet<SyncConfiguration> SyncConfigurations { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<UserGroupMembership> UserGroupMemberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // LdapServer configuration
        modelBuilder.Entity<LdapServer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Host).HasMaxLength(512).IsRequired();
            entity.Property(e => e.BaseDn).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.BindDn).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.BindPassword).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.UserSearchFilter).HasMaxLength(2048);
            entity.Property(e => e.GroupSearchFilter).HasMaxLength(2048);
            entity.Property(e => e.ServerType).HasMaxLength(64);
            entity.Property(e => e.Description).HasMaxLength(2048);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
        });

        // LdapUser configuration
        modelBuilder.Entity<LdapUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.Uid).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CommonName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(512).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.TelephoneNumber).HasMaxLength(64);
            entity.Property(e => e.Ou).HasMaxLength(256);
            entity.Property(e => e.Organization).HasMaxLength(256);
            entity.Property(e => e.LoginShell).HasMaxLength(256);
            entity.Property(e => e.HomeDirectory).HasMaxLength(512);
            entity.Property(e => e.DistinguishedName).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(1024);
            entity.HasIndex(e => e.DistinguishedName).HasPrefixLength(512).IsUnique();
            entity.HasIndex(e => e.CommonName).HasPrefixLength(255);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.GidNumber);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Uid);

            entity.HasMany(e => e.GroupMemberships)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LdapGroup configuration
        modelBuilder.Entity<LdapGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.CommonName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.Property(e => e.DistinguishedName).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.ObjectClass).HasMaxLength(128);
            entity.HasIndex(e => e.DistinguishedName).HasPrefixLength(512).IsUnique();
            entity.HasIndex(e => e.CommonName).HasPrefixLength(255);
            entity.HasIndex(e => e.GidNumber);
            entity.HasIndex(e => e.IsActive);

            entity.HasMany(e => e.UserMemberships)
                  .WithOne(e => e.Group)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SyncConfiguration configuration
        modelBuilder.Entity<SyncConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.ServerId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.CronSchedule).HasMaxLength(128);
            entity.Property(e => e.SyncMode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SearchBase).HasMaxLength(1024);
            entity.Property(e => e.ExcludedAttributes).HasMaxLength(2048);
            entity.Property(e => e.LastSyncStatus).HasMaxLength(64);
            entity.Property(e => e.LastSyncError).HasMaxLength(2048);
            entity.HasIndex(e => e.ServerId);
            entity.HasIndex(e => e.Enabled);

            entity.HasOne(e => e.Server)
                  .WithOne(e => e.SyncConfiguration)
                  .HasForeignKey<SyncConfiguration>(e => e.ServerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SyncLog configuration
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
        });

        // UserGroupMembership configuration
        modelBuilder.Entity<UserGroupMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.UserId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.GroupId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.MemberAttributeType).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => new { e.UserId, e.GroupId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(e => e.GroupMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Group)
                  .WithMany(e => e.UserMemberships)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
