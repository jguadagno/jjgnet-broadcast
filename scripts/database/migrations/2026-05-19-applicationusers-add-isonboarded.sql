-- Migration: Add IsOnboarded flag to ApplicationUsers
-- Applied: 2026-05-19
-- Purpose: Cache whether the user has completed all three onboarding areas
--          (collector, publisher, message templates) so the setup banner can
--          be bypassed with zero API calls on fully-onboarded users.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationUsers]')
      AND name = N'IsOnboarded'
)
BEGIN
    ALTER TABLE [dbo].[ApplicationUsers]
    ADD [IsOnboarded] BIT NOT NULL DEFAULT 0;
END
