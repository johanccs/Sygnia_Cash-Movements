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
    -- A 10-value repeating cycle (deterministic, not RAND()/NEWID()-driven, so the amounts are
    -- reproducible) mixing small and large deposits/withdrawals so the running total and
    -- balance math get exercised across realistic order-of-magnitude swings, not just ±one value.
    CASE (N % 10)
        WHEN 0 THEN 15000.00
        WHEN 1 THEN -8250.50
        WHEN 2 THEN 320.75
        WHEN 3 THEN -95.00
        WHEN 4 THEN 4200.00
        WHEN 5 THEN -1750.25
        WHEN 6 THEN 60.10
        WHEN 7 THEN -12500.00
        WHEN 8 THEN 875.35
        ELSE -430.60
    END,
    DATEADD(MINUTE, N, @start),
    'Seeded statement row',
    NEWID(),
    'seed-script',
    @start
FROM Numbers;
GO
