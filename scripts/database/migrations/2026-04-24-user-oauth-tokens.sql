use JJGNet
go

-- Per-user OAuth token storage for LinkedIn (and future social platforms).
-- Access tokens are stored as nvarchar(max) — SQL Server TDE provides OS-level at-rest encryption.
-- Follow-up: evaluate SQL Server Always Encrypted for AccessToken / RefreshToken columns if compliance requirements arise.
create table dbo.UserOAuthTokens
(
    Id                    int identity
        constraint PK_UserOAuthTokens
            primary key clustered,
    CreatedByEntraOid     nvarchar(36)   not null,
    SocialMediaPlatformId int            not null
        constraint FK_UserOAuthTokens_SocialMediaPlatforms
            references dbo.SocialMediaPlatforms,
    AccessToken           nvarchar(max)  not null,
    RefreshToken          nvarchar(max)  null,
    AccessTokenExpiresAt  datetimeoffset not null,
    RefreshTokenExpiresAt datetimeoffset null,
    CreatedOn             datetimeoffset not null
        constraint DF_UserOAuthTokens_CreatedOn
            default (getutcdate()),
    LastUpdatedOn         datetimeoffset not null
        constraint DF_UserOAuthTokens_LastUpdatedOn
            default (getutcdate()),
    constraint UQ_UserOAuthTokens_User_Platform
        unique (CreatedByEntraOid, SocialMediaPlatformId)
)
go

create nonclustered index IX_UserOAuthTokens_AccessTokenExpiresAt
    on dbo.UserOAuthTokens (AccessTokenExpiresAt)
go
