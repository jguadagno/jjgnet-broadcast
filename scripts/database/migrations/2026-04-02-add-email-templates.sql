-- Migration: 20260404-add-email-templates.sql
-- Issue: #615
-- Date: 2026-04-04
-- Description: Add EmailTemplates table and seed data for user approval notification emails

USE JJGNet;
GO

-- ============================================================
-- Create EmailTemplates table
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailTemplates' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[EmailTemplates] (
        [Id]          INT             IDENTITY(1,1) NOT NULL,
        [Name]        NVARCHAR(100)   NOT NULL,
        [Subject]     NVARCHAR(500)   NOT NULL,
        [Body]        NVARCHAR(MAX)   NOT NULL,
        [CreatedDate] DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedDate] DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- ============================================================
-- Create unique index on Name
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EmailTemplates_Name' AND object_id = OBJECT_ID('dbo.EmailTemplates'))
BEGIN
    CREATE UNIQUE INDEX [UQ_EmailTemplates_Name] ON [dbo].[EmailTemplates] ([Name]);
END
GO

-- ============================================================
-- Seed data: user approval notification templates
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailTemplates] WHERE [Name] = 'UserApproved')
BEGIN
    INSERT INTO [dbo].[EmailTemplates] ([Name], [Subject], [Body]) VALUES (
        N'UserApproved',
        N'Your account has been approved',
        N'<!DOCTYPE html>
<html>
<body>
<p>Hello,</p>
<p>Great news! Your account has been approved and you now have access to the JJGNet Broadcasting application.</p>
<p>You can log in at any time using your Microsoft account.</p>
<p>If you have any questions, please reach out to the administrator.</p>
<p>Welcome aboard!</p>
<p>The JJGNet Broadcasting Team</p>
</body>
</html>'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailTemplates] WHERE [Name] = 'UserRejected')
BEGIN
    INSERT INTO [dbo].[EmailTemplates] ([Name], [Subject], [Body]) VALUES (
        N'UserRejected',
        N'Your account access request',
        N'<!DOCTYPE html>
<html>
<body>
<p>Hello,</p>
<p>Thank you for your interest in the JJGNet Broadcasting application.</p>
<p>After review, we are unable to approve your access request at this time.</p>
<p>If you believe this is an error or would like more information, please contact the administrator.</p>
<p>The JJGNet Broadcasting Team</p>
</body>
</html>'
    );
END
GO
