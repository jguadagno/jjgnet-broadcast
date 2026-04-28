-- Migration: 2026-05-02-add-linkedin-expiry-email-templates
-- Issue: #853
-- Description: Seed LinkedIn OAuth token expiry notification email templates

IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailTemplates] WHERE [Name] = N'LinkedInTokenExpiring7Day')
    INSERT INTO [dbo].[EmailTemplates] ([Name], [Subject], [Body]) VALUES (
        N'LinkedInTokenExpiring7Day',
        N'Action required: Your LinkedIn connection expires in 7 days',
        N'<!DOCTYPE html><html><body><p>Hello {{ display_name }},</p><p>Your LinkedIn OAuth token will expire on <strong>{{ expires_at }}</strong> — that is in approximately 7 days.</p><p>To keep your LinkedIn posts publishing without interruption, please re-authorise your LinkedIn connection before it expires.</p><p><a href="{{ reauth_url }}">Re-connect your LinkedIn account</a></p><p>If you have already re-connected your account, you can ignore this message.</p><p>The JJGNet Broadcasting Team</p></body></html>'
    );

IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailTemplates] WHERE [Name] = N'LinkedInTokenExpiring1Day')
    INSERT INTO [dbo].[EmailTemplates] ([Name], [Subject], [Body]) VALUES (
        N'LinkedInTokenExpiring1Day',
        N'Urgent: Your LinkedIn connection expires tomorrow',
        N'<!DOCTYPE html><html><body><p>Hello {{ display_name }},</p><p>Your LinkedIn OAuth token will expire on <strong>{{ expires_at }}</strong> — that is tomorrow.</p><p>Please re-authorise your LinkedIn connection immediately to avoid any disruption to your scheduled posts.</p><p><a href="{{ reauth_url }}">Re-connect your LinkedIn account now</a></p><p>If you have already re-connected your account, you can ignore this message.</p><p>The JJGNet Broadcasting Team</p></body></html>'
    );
