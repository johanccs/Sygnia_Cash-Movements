-- Seeds 2 accounts. Run against sygnia_cash after migrations (dotnet ef database update
-- or Migrations/InitialCreate.sql) have created the schema. Safe to re-run — skips rows that
-- already exist by primary key.
USE sygnia_cash;
GO

IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = 'ACC-001')
BEGIN
    INSERT INTO Accounts (AccountId, AccountName, ContactPerson, Currency, CreatedDate, CreatedBy)
    VALUES ('ACC-001', 'Acme Holdings', 'Jane Smith', 'ZAR', SYSUTCDATETIME(), 'seed-script');
END

IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = 'ACC-002')
BEGIN
    INSERT INTO Accounts (AccountId, AccountName, ContactPerson, Currency, CreatedDate, CreatedBy)
    VALUES ('ACC-002', 'Beacon Traders', 'John Doe', 'ZAR', SYSUTCDATETIME(), 'seed-script');
END
GO
