namespace Sygnia.Tests.Infrastructure.Integration;

/// <summary>
/// One SQL Server container shared across every integration test class — starting a fresh
/// container per class would be minutes, not seconds, per test run.
/// </summary>
[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
