-- Migration: Bound previously-unbounded NVARCHAR columns for indexing
-- Issue: #322
-- Date: 2026-04-07
-- 
-- Convert NVARCHAR(MAX) to bounded lengths on filterable columns to enable
-- non-clustered index creation. SQL Server requires bounded column lengths
-- for index keys.

USE JJGNet;
GO

-- Engagements table
-- Name: NVARCHAR(MAX) → NVARCHAR(500)
-- Url: NVARCHAR(MAX) → NVARCHAR(2048)

IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Engagements')
    AND c.name = 'Name'
    AND t.name = 'nvarchar'
    AND c.max_length = -1  -- -1 indicates MAX
)
BEGIN
    PRINT 'Altering Engagements.Name to NVARCHAR(500)';
    ALTER TABLE dbo.Engagements
        ALTER COLUMN [Name] NVARCHAR(500) NOT NULL;
END
ELSE
BEGIN
    PRINT 'Engagements.Name already bounded or does not exist';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Engagements')
    AND c.name = 'Url'
    AND t.name = 'nvarchar'
    AND c.max_length = -1  -- -1 indicates MAX
)
BEGIN
    PRINT 'Altering Engagements.Url to NVARCHAR(2048)';
    ALTER TABLE dbo.Engagements
        ALTER COLUMN [Url] NVARCHAR(2048) NULL;
END
ELSE
BEGIN
    PRINT 'Engagements.Url already bounded or does not exist';
END
GO

-- Talks table
-- Name: NVARCHAR(MAX) → NVARCHAR(500)
-- TalkLocation: NVARCHAR(MAX) → NVARCHAR(500)

IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Talks')
    AND c.name = 'Name'
    AND t.name = 'nvarchar'
    AND c.max_length = -1  -- -1 indicates MAX
)
BEGIN
    PRINT 'Altering Talks.Name to NVARCHAR(500)';
    ALTER TABLE dbo.Talks
        ALTER COLUMN [Name] NVARCHAR(500) NOT NULL;
END
ELSE
BEGIN
    PRINT 'Talks.Name already bounded or does not exist';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Talks')
    AND c.name = 'TalkLocation'
    AND t.name = 'nvarchar'
    AND c.max_length = -1  -- -1 indicates MAX
)
BEGIN
    PRINT 'Altering Talks.TalkLocation to NVARCHAR(500)';
    ALTER TABLE dbo.Talks
        ALTER COLUMN [TalkLocation] NVARCHAR(500) NULL;
END
ELSE
BEGIN
    PRINT 'Talks.TalkLocation already bounded or does not exist';
END
GO

PRINT 'Migration completed: NVARCHAR bounded columns (#322)';
GO
