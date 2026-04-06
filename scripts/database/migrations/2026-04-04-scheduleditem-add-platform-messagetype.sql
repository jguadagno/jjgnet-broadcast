-- Migration: Add Platform and MessageType columns to ScheduledItems
-- Issue: #89 - Refactor scheduled items to pre-compose content series
-- Date: 2026-04-04
-- Author: Morpheus (Squad)
--
-- Purpose: Add Platform and MessageType columns to ScheduledItems table to support
-- pre-composed message series for different social platforms.
--
-- This enables the new pattern where:
-- 1. On new Engagement/Talk creation, a content publisher generates a series of scheduled items
-- 2. Each scheduled item is pre-composed with platform-specific content
-- 3. When scheduled time fires, the system publishes the already-composed message
--
-- Platform valid values: 'Twitter', 'Facebook', 'LinkedIn', 'Bluesky'
-- MessageType examples: 'RandomPost', 'Speaking90Days', 'Speaking30Days', 'NextWeek', 'Tomorrow', 'ComingUp'

USE JJGNet;
GO

-- Add Platform column (nullable initially for existing data)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ScheduledItems') AND name = 'Platform')
BEGIN
    ALTER TABLE dbo.ScheduledItems
    ADD Platform NVARCHAR(50) NULL;
    
    PRINT 'Added Platform column to ScheduledItems';
END
ELSE
BEGIN
    PRINT 'Platform column already exists in ScheduledItems';
END
GO

-- Add MessageType column (nullable initially for existing data)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ScheduledItems') AND name = 'MessageType')
BEGIN
    ALTER TABLE dbo.ScheduledItems
    ADD MessageType NVARCHAR(50) NULL;
    
    PRINT 'Added MessageType column to ScheduledItems';
END
ELSE
BEGIN
    PRINT 'MessageType column already exists in ScheduledItems';
END
GO

-- Backfill existing records with default values
-- Existing scheduled items without a platform can be marked as 'Legacy' for identification
UPDATE dbo.ScheduledItems
SET Platform = 'Legacy', MessageType = 'Legacy'
WHERE Platform IS NULL OR MessageType IS NULL;
GO

PRINT 'Migration 2026-04-04-scheduleditem-add-platform-messagetype.sql completed successfully';
GO
