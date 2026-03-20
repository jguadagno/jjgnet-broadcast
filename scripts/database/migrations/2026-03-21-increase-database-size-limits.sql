-- Migration: Increase database size limits
-- Date: 2026-03-21
-- Issue: #324
-- Description: Remove 50MB MAXSIZE cap from data file and 25MB cap from log file
--              to prevent silent INSERT failures when capacity is reached.
--
-- Note: This script uses MODIFY FILE which does not require database recreation.
--       It can be run on an existing database without data loss.

USE master;
GO

ALTER DATABASE JJGNet
MODIFY FILE (
    NAME = JJGNet_Data,
    MAXSIZE = UNLIMITED
);
GO

ALTER DATABASE JJGNet
MODIFY FILE (
    NAME = JJGNet_Log,
    MAXSIZE = UNLIMITED
);
GO

-- Verify the changes
SELECT 
    name,
    physical_name,
    CAST(size * 8.0 / 1024 AS DECIMAL(10,2)) AS SizeMB,
    CASE 
        WHEN max_size = -1 THEN 'UNLIMITED'
        WHEN max_size = 268435456 THEN 'UNLIMITED'
        ELSE CAST(max_size * 8.0 / 1024 AS VARCHAR(20)) + ' MB'
    END AS MaxSizeMB,
    CASE 
        WHEN is_percent_growth = 1 THEN CAST(growth AS VARCHAR(10)) + '%'
        ELSE CAST(growth * 8.0 / 1024 AS VARCHAR(20)) + ' MB'
    END AS GrowthMB
FROM sys.master_files
WHERE database_id = DB_ID('JJGNet');
GO
