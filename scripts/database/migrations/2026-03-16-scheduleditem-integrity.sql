-- Migration: ScheduledItems Referential Integrity (Issue #274)
-- Date: 2026-03-16
--
-- 1. Fix bad data introduced by UI mismatch (wrong ItemTableName values)
-- 2. Add CHECK constraint enforcing valid ItemTableName values

-- Fix rows where ItemTableName was incorrectly set by the UI
UPDATE dbo.ScheduledItems SET ItemTableName = 'SyndicationFeedItems' WHERE ItemTableName = 'SyndicationFeed';
UPDATE dbo.ScheduledItems SET ItemTableName = 'YouTubeItems'         WHERE ItemTableName = 'YouTube';

-- Add CHECK constraint: only known source table names are allowed
ALTER TABLE dbo.ScheduledItems
    ADD CONSTRAINT CK_ScheduledItems_ItemTableName
    CHECK (ItemTableName IN ('Engagements', 'Talks', 'SyndicationFeedItems', 'YouTubeItems'));
go
