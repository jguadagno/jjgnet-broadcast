-- Migration: Fix datetime2 → datetimeoffset for RBAC tables
-- Affected tables: ApplicationUsers (CreatedAt, UpdatedAt), UserApprovalLog (CreatedAt)
-- These columns were created with datetime2 but EF entities use DateTimeOffset,
-- causing runtime exceptions on read/write.

-- ApplicationUsers.CreatedAt
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ApplicationUsers')
      AND name = 'CreatedAt'
      AND system_type_id = TYPE_ID('datetime2')
)
BEGIN
    ALTER TABLE dbo.ApplicationUsers
        ALTER COLUMN CreatedAt datetimeoffset NOT NULL;
    PRINT 'ApplicationUsers.CreatedAt converted to datetimeoffset';
END
GO

-- ApplicationUsers.UpdatedAt
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ApplicationUsers')
      AND name = 'UpdatedAt'
      AND system_type_id = TYPE_ID('datetime2')
)
BEGIN
    ALTER TABLE dbo.ApplicationUsers
        ALTER COLUMN UpdatedAt datetimeoffset NOT NULL;
    PRINT 'ApplicationUsers.UpdatedAt converted to datetimeoffset';
END
GO

-- UserApprovalLog.CreatedAt
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.UserApprovalLog')
      AND name = 'CreatedAt'
      AND system_type_id = TYPE_ID('datetime2')
)
BEGIN
    ALTER TABLE dbo.UserApprovalLog
        ALTER COLUMN CreatedAt datetimeoffset NOT NULL;
    PRINT 'UserApprovalLog.CreatedAt converted to datetimeoffset';
END
GO
