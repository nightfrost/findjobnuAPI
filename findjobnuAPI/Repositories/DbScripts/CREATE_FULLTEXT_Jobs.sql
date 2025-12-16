USE [FindjobnuDB];
GO

-- Ensure full-text is enabled for the database (no-op if it already is)
IF (SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')) = 1
BEGIN
    IF DATABASEPROPERTYEX(DB_NAME(), 'IsFullTextEnabled') <> 1
    BEGIN
        EXEC sp_fulltext_database 'enable';
    END
END
ELSE
BEGIN
    THROW 50000, 'Full-Text Search feature is not installed on this SQL Server instance.', 1;
END
GO

-- Shared catalog for job posting entities
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'FTCatalog_JobIndex')
BEGIN
    CREATE FULLTEXT CATALOG FTCatalog_JobIndex WITH ACCENT_SENSITIVITY = OFF;
END
GO

-- Unique key for CONTAINSTABLE queries on JobIndexPostingsExtended
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_JobIndexPostingsExtended_FT' AND object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_JobIndexPostingsExtended_FT ON dbo.JobIndexPostingsExtended(JobID);
END
GO

-- Unique key for CONTAINSTABLE queries on JobKeywords
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_JobKeywords_FT' AND object_id = OBJECT_ID('dbo.JobKeywords'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_JobKeywords_FT ON dbo.JobKeywords(KeywordID);
END
GO

-- Recreate the full-text index on JobIndexPostingsExtended (JobIndexPostsService.SearchAsync & GetRecommendedJobsByUserAndProfile)
IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    DROP FULLTEXT INDEX ON dbo.JobIndexPostingsExtended;
END
GO

CREATE FULLTEXT INDEX ON dbo.JobIndexPostingsExtended
(
    JobTitle LANGUAGE 1033,
    JobDescription LANGUAGE 1033,
    CompanyName LANGUAGE 1033,
    JobLocation LANGUAGE 1033
)
KEY INDEX UX_JobIndexPostingsExtended_FT
ON FTCatalog_JobIndex
WITH CHANGE_TRACKING AUTO, STOPLIST = SYSTEM;
GO

-- Recreate the full-text index backing JobKeywords lookups
IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.JobKeywords'))
BEGIN
    DROP FULLTEXT INDEX ON dbo.JobKeywords;
END
GO

CREATE FULLTEXT INDEX ON dbo.JobKeywords
(
    Keyword LANGUAGE 1033
)
KEY INDEX UX_JobKeywords_FT
ON FTCatalog_JobIndex
WITH CHANGE_TRACKING AUTO, STOPLIST = SYSTEM;
GO

-- Supporting nonclustered indexes used by filtering/pagination paths
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobIndexPostingsExtended_Published' AND object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_JobIndexPostingsExtended_Published
        ON dbo.JobIndexPostingsExtended(Published DESC)
        INCLUDE (JobTitle, JobLocation, CompanyName);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobKeywords_JobID' AND object_id = OBJECT_ID('dbo.JobKeywords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_JobKeywords_JobID ON dbo.JobKeywords(JobID) INCLUDE (Keyword);
END
GO

PRINT 'Full-text catalog and indexes for JobIndexPostingsExtended and JobKeywords are now configured.';
