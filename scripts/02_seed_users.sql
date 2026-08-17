-- Seeds 3 users. Safe to re-run — skips rows that already exist by primary key.
USE sygnia_cash;
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 'jsmith')
BEGIN
    INSERT INTO Users (Id, Name, Surname) VALUES ('jsmith', 'Jane', 'Smith');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 'jdoe')
BEGIN
    INSERT INTO Users (Id, Name, Surname) VALUES ('jdoe', 'John', 'Doe');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 'mwilliams')
BEGIN
    INSERT INTO Users (Id, Name, Surname) VALUES ('mwilliams', 'Mary', 'Williams');
END
GO
