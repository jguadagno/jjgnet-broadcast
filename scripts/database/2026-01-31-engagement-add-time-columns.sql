ALTER TABLE dbo.Engagements
    ADD CreatedOn datetime2 default getutcdate() NOT NULL,
        LastUpdatedOn datetime2 default getutcdate() NOT NULL;
