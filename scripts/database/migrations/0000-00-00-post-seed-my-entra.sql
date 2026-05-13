DECLARE @adminOid NVARCHAR(36) = '<insert Entra OID';
UPDATE ApplicationUsers SET ApprovalStatus = 'Approved' WHERE EntraObjectId = @adminOid
UPDATE UserRoles SET RoleId = 1 where UserId = 1
UPDATE Engagements SET CreatedByEntraOid = @adminOid
UPDATE MessageTemplates SET CreatedByEntraOid = @adminOid
UPDATE ScheduledItems SET CreatedByEntraOid = @adminOid
UPDATE SyndicationFeedItems SET CreatedByEntraOid = @adminOid
UPDATE Talks SET CreatedByEntraOid = @adminOid
UPDATE UserOAuthTokens SET CreatedByEntraOid = @adminOid
UPDATE YouTubeItems SET CreatedByEntraOid = @adminOid
