using MediView.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(320);
        builder.Property(user => user.PasswordHash).HasMaxLength(512);
        builder.Property(user => user.FullName).HasMaxLength(200);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasXminConcurrencyToken();
    }
}
