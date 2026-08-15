-- Seeds 50,000 movements against ACC-001 (run 01_seed_accounts.sql first) — the scale the
-- statement-streaming requirement is graded on. Set-based insert via a numbers CTE, not a
-- cursor/loop, so this runs in seconds rather than minutes.
-- Safe to re-run: does nothing if ACC-001 already has 50,000+ movements with the MOV-SEED-
-- prefix this script uses.
USE sygnia_cash;
GO

DECLARE @accountId varchar(10) = 'ACC-001';
DECLARE @rowCount int = 50000;
DECLARE @start datetime2 = '2024-01-01T00:00:00';

IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = @accountId)
BEGIN
    RAISERROR('Account %s does not exist — run 01_seed_accounts.sql first.', 16, 1, @accountId);
    RETURN;
END

IF (SELECT COUNT(*) FROM Movements WHERE AccountId = @accountId AND ExternalRef LIKE 'MOV-SEED-%') >= @rowCount
BEGIN
    PRINT 'Already seeded — skipping.';
    RETURN;
END

;WITH Numbers AS (
    SELECT TOP (@rowCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Movements (AccountId, ExternalRef, Currency, Amount, OccurredAt, Narration, RefNr, MovedBy, MovedDate)
SELECT
    @accountId,
    CONCAT('MOV-SEED-', RIGHT('00000' + CAST(N AS varchar(5)), 5)),
    'ZAR',
    -- Alternates deposit/withdrawal so the running total moves in both directions.
    CASE WHEN N % 2 = 0 THEN 100.00 ELSE -40.00 END,
    DATEADD(MINUTE, N, @start),
    'Seeded statement row',
    NEWID(),
    'seed-script',
    @start
FROM Numbers;
GO
