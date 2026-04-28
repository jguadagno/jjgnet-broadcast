-- Migration: 2026-05-01-user-oauth-tokens-add-last-notified-at.sql
-- Adds LastNotifiedAt column to dbo.UserOAuthTokens for LinkedIn token expiry notifications (Issue #852).
-- Idempotent: safe to run multiple times.

USE JJGNet;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.UserOAuthTokens')
      AND name = 'LastNotifiedAt'
)
BEGIN
    ALTER TABLE dbo.UserOAuthTokens
        ADD LastNotifiedAt datetimeoffset NULL;
END
GO
