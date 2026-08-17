IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815094528_InitialCreate'
)
BEGIN
    CREATE TABLE [Accounts] (
        [AccountId] nvarchar(10) NOT NULL,
        [AccountName] nvarchar(20) NOT NULL,
        [ContactPerson] nvarchar(30) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815094528_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(50) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Surname] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815094528_InitialCreate'
)
BEGIN
    CREATE TABLE [Movements] (
        [AccountId] nvarchar(10) NOT NULL,
        [ExternalRef] nvarchar(20) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Amount] DECIMAL(19,4) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [Narration] nvarchar(200) NULL,
        [RefNr] uniqueidentifier NOT NULL,
        [MovedBy] nvarchar(50) NOT NULL,
        [MovedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Movements] PRIMARY KEY ([AccountId], [ExternalRef]),
        CONSTRAINT [FK_Movements_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815094528_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Movements_Account_OccurredAt] ON [Movements] ([AccountId], [OccurredAt]) INCLUDE ([Amount]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815094528_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260815094528_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815123315_AddAccountCurrency'
)
BEGIN
    ALTER TABLE [Accounts] ADD [Currency] nvarchar(3) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815123315_AddAccountCurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260815123315_AddAccountCurrency', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816203857_AddMovementConflictAudits'
)
BEGIN
    CREATE TABLE [MovementConflictAudits] (
        [Id] int NOT NULL IDENTITY,
        [AccountId] nvarchar(10) NOT NULL,
        [ExternalRef] nvarchar(20) NOT NULL,
        [AttemptedAmount] DECIMAL(19,4) NOT NULL,
        [AttemptedCurrency] nvarchar(3) NOT NULL,
        [AttemptedOccurredAt] datetime2 NOT NULL,
        [StoredAmount] DECIMAL(19,4) NOT NULL,
        [StoredCurrency] nvarchar(3) NOT NULL,
        [StoredOccurredAt] datetime2 NOT NULL,
        [ConflictingFields] nvarchar(200) NOT NULL,
        [DetectedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MovementConflictAudits] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816203857_AddMovementConflictAudits'
)
BEGIN
    CREATE INDEX [IX_MovementConflictAudits_AccountId_ExternalRef] ON [MovementConflictAudits] ([AccountId], [ExternalRef]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260816203857_AddMovementConflictAudits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260816203857_AddMovementConflictAudits', N'8.0.10');
END;
GO

COMMIT;
GO

