-- Migration: Add RBAC and User Approval database schema
-- Issue: #602
-- Date: 2026-04-02
-- Description: Phase 1 of RBAC implementation. Creates tables for user approval workflow
--              and role-based access control using Entra ID oid claim as the user key.
--              Supports future multi-tenancy by keying users on Entra Object ID.

USE JJGNet;
GO

-- ============================================================
-- Table 1: Roles lookup table
-- ============================================================
CREATE TABLE [dbo].[Roles] (
    [Id]          INT            NOT NULL IDENTITY(1,1),
    [Name]        NVARCHAR(50)   NOT NULL,
    [Description] NVARCHAR(200)  NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Roles_Name] UNIQUE ([Name])
);
GO

-- ============================================================
-- Table 2: ApplicationUsers — local user record keyed on Entra Object ID (oid claim)
-- ============================================================
-- NOTE: This table is designed to support future multi-tenancy.
-- EntraObjectId is the Entra ID oid claim — stable per-user GUID that never changes.
CREATE TABLE [dbo].[ApplicationUsers] (
    [Id]             INT            NOT NULL IDENTITY(1,1),
    [EntraObjectId]  NVARCHAR(36)   NOT NULL,   -- Entra oid claim
    [DisplayName]    NVARCHAR(200)  NULL,
    [Email]          NVARCHAR(200)  NULL,
    [ApprovalStatus] NVARCHAR(20)   NOT NULL CONSTRAINT [DF_ApplicationUsers_ApprovalStatus] DEFAULT ('Pending'),
                     -- Valid values: 'Pending', 'Approved', 'Rejected'
    [ApprovalNotes]  NVARCHAR(500)  NULL,        -- Required when ApprovalStatus = 'Rejected'
    [CreatedAt]      DATETIME2      NOT NULL CONSTRAINT [DF_ApplicationUsers_CreatedAt] DEFAULT (GETUTCDATE()),
    [UpdatedAt]      DATETIME2      NOT NULL CONSTRAINT [DF_ApplicationUsers_UpdatedAt] DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_ApplicationUsers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ApplicationUsers_EntraObjectId] UNIQUE ([EntraObjectId])
);
GO

-- ============================================================
-- Table 3: UserRoles — many-to-many join
-- ============================================================
CREATE TABLE [dbo].[UserRoles] (
    [UserId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUsers] ([Id]),
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id])
);
GO

-- ============================================================
-- Table 4: UserApprovalLog — audit trail
-- ============================================================
CREATE TABLE [dbo].[UserApprovalLog] (
    [Id]          INT            NOT NULL IDENTITY(1,1),
    [UserId]      INT            NOT NULL,
    [AdminUserId] INT            NULL,   -- NULL for system-generated entries
    [Action]      NVARCHAR(20)   NOT NULL,
                  -- Valid values: 'Registered', 'Approved', 'Rejected', 'RoleAssigned', 'RoleRemoved'
    [Notes]       NVARCHAR(500)  NULL,
    [CreatedAt]   DATETIME2      NOT NULL CONSTRAINT [DF_UserApprovalLog_CreatedAt] DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_UserApprovalLog] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserApprovalLog_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUsers] ([Id]),
    CONSTRAINT [FK_UserApprovalLog_Admin] FOREIGN KEY ([AdminUserId]) REFERENCES [dbo].[ApplicationUsers] ([Id])
);
GO

-- ============================================================
-- Seed data: Three roles
-- ============================================================
-- Idempotent seed — do not duplicate
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Administrator')
    INSERT INTO [dbo].[Roles] ([Name], [Description])
    VALUES ('Administrator', 'Full access; can approve users, manage roles, and delete any content');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Contributor')
    INSERT INTO [dbo].[Roles] ([Name], [Description])
    VALUES ('Contributor', 'Can create, edit, and delete their own content');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Viewer')
    INSERT INTO [dbo].[Roles] ([Name], [Description])
    VALUES ('Viewer', 'Read-only access to content');
GO

-- ============================================================
-- CHECK constraints (enforce enum values at DB level)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_ApplicationUsers_ApprovalStatus'
      AND parent_object_id = OBJECT_ID('dbo.ApplicationUsers'))
BEGIN
    ALTER TABLE [dbo].[ApplicationUsers]
        ADD CONSTRAINT [CK_ApplicationUsers_ApprovalStatus]
            CHECK ([ApprovalStatus] IN ('Pending', 'Approved', 'Rejected'));
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_UserApprovalLog_Action'
      AND parent_object_id = OBJECT_ID('dbo.UserApprovalLog'))
BEGIN
    ALTER TABLE [dbo].[UserApprovalLog]
        ADD CONSTRAINT [CK_UserApprovalLog_Action]
            CHECK ([Action] IN ('Registered', 'Approved', 'Rejected', 'RoleAssigned', 'RoleRemoved'));
END
GO

-- ============================================================
-- ADMINISTRATOR SEED (manual step — DO NOT automate)
-- ============================================================
-- After running this migration, seed the initial Administrator
-- by running the following with the correct Entra Object ID
-- from your application configuration (e.g., appsettings.json
-- or Azure Key Vault):
--
-- DECLARE @adminOid NVARCHAR(36) = '<value from config>';
-- INSERT INTO [dbo].[ApplicationUsers] ([EntraObjectId], [DisplayName], [Email], [ApprovalStatus])
-- VALUES (@adminOid, 'Administrator', '', 'Approved');
--
-- INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
-- SELECT u.[Id], r.[Id]
-- FROM [dbo].[ApplicationUsers] u
-- CROSS JOIN [dbo].[Roles] r
-- WHERE u.[EntraObjectId] = @adminOid AND r.[Name] = 'Administrator';
--
-- INSERT INTO [dbo].[UserApprovalLog] ([UserId], [Action], [Notes])
-- SELECT [Id], 'Approved', 'Initial administrator seed'
-- FROM [dbo].[ApplicationUsers]
-- WHERE [EntraObjectId] = @adminOid;
-- ============================================================
