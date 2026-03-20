-- Migration: Add BlueSkyHandle to Engagements and Talks tables
-- Issues: #167 (Engagement), #166 (Scheduled Talk)
-- Date: 2026-03-21

USE JJGNet;
GO

ALTER TABLE dbo.Engagements
    ADD BlueSkyHandle NVARCHAR(255) NULL;
GO

ALTER TABLE dbo.Talks
    ADD BlueSkyHandle NVARCHAR(255) NULL;
GO
