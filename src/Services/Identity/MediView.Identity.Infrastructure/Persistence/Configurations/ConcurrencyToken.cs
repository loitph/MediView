using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Identity.Infrastructure.Persistence.Configurations;

internal static class ConcurrencyToken
{
    public static EntityTypeBuilder<T> HasXminConcurrencyToken<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<uint>("Version").IsRowVersion();
        return builder;
    }
}
