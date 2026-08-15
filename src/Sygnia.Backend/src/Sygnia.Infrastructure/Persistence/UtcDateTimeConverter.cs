using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sygnia.Infrastructure.Persistence;

/// <summary>Stores a UTC <see cref="DateTime"/> as-is; stamps <see cref="DateTimeKind.Utc"/> back on read.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
