-- Migration: Role Restructure (Issue #719)
-- Renames the existing 'Administrator' role to 'Site Administrator' and introduces
-- a new narrower 'Administrator' role for personal content management.
-- Safe to replay: all statements are idempotent.

USE JJGNet;
GO

-- Rename existing Administrator → Site Administrator if needed (existing environments)
IF EXISTS (SELECT 1 FROM JJGNet.dbo.Roles WHERE Name = N'Administrator')
   AND NOT EXISTS (SELECT 1 FROM JJGNet.dbo.Roles WHERE Name = N'Site Administrator')
    UPDATE JJGNet.dbo.Roles
    SET Name = N'Site Administrator',
        Description = N'Full app admin: user approval, role management, and global platform definitions'
    WHERE Name = N'Administrator'
GO

-- Ensure Site Administrator exists (fresh environments or missed UPDATE above)
IF NOT EXISTS (SELECT 1 FROM JJGNet.dbo.Roles WHERE Name = N'Site Administrator')
    INSERT INTO JJGNet.dbo.Roles (Name, Description)
    VALUES (N'Site Administrator', N'Full app admin: user approval, role management, and global platform definitions')
GO

-- Ensure new narrower Administrator role exists
IF NOT EXISTS (SELECT 1 FROM JJGNet.dbo.Roles WHERE Name = N'Administrator')
    INSERT INTO JJGNet.dbo.Roles (Name, Description)
    VALUES (N'Administrator', N'Personal content admin: manage own Message Templates and Social Media Platforms')
GO
