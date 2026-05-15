-- Migration: Drop the legacy UserPublisherSettings table
-- This table was replaced by per-publisher typed tables created in the 2026-05-15-publisher-settings-per-publisher-tables.sql migration.
-- Safe to run multiple times (idempotent).

IF OBJECT_ID(N'dbo.UserPublisherSettings', N'U') IS NOT NULL
    DROP TABLE dbo.UserPublisherSettings;
