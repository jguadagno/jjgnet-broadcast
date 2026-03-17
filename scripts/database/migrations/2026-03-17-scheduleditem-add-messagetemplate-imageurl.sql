-- Migration: ScheduledItems Add MessageTemplate and ImageUrl (Issue #269)
-- Date: 2026-03-17
--
-- 1. Add MessageTemplate column (NVARCHAR(MAX), nullable) for Scriban broadcast message templates
-- 2. Add ImageUrl column (NVARCHAR(2048), nullable) for image URLs to include in broadcasts

ALTER TABLE dbo.ScheduledItems
    ADD MessageTemplate nvarchar(max) NULL;
go

ALTER TABLE dbo.ScheduledItems
    ADD ImageUrl nvarchar(2048) NULL;
go
