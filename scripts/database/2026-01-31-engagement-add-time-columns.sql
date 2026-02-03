ALTER TABLE dbo.Engagements
    ADD CreatedOn datetimeoffset default getutcdate() NOT NULL,
        LastUpdatedOn datetimeoffset default getutcdate() NOT NULL;
