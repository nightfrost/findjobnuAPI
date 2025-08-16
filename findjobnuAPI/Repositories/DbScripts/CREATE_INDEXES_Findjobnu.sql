-- Recommended indexes to speed up search and recommendations
-- Run on your Findjobnu database

-- JobIndexPostingsExtended indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobIndexPostingsExtended_CompanyName' AND object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    CREATE INDEX IX_JobIndexPostingsExtended_CompanyName
    ON dbo.JobIndexPostingsExtended(CompanyName)
    INCLUDE (JobID);
END
GO

-- If JobTitle is a MAX type, create a persisted computed column for indexing
IF COL_LENGTH('dbo.JobIndexPostingsExtended','JobTitleShort') IS NULL
BEGIN
    ALTER TABLE dbo.JobIndexPostingsExtended
    ADD JobTitleShort AS CAST(LEFT(JobTitle, 450) AS NVARCHAR(450)) PERSISTED;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobIndexPostingsExtended_JobTitleShort' AND object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    CREATE INDEX IX_JobIndexPostingsExtended_JobTitleShort
    ON dbo.JobIndexPostingsExtended(JobTitleShort)
    INCLUDE (JobID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobIndexPostingsExtended_JobLocation' AND object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    CREATE INDEX IX_JobIndexPostingsExtended_JobLocation
    ON dbo.JobIndexPostingsExtended(JobLocation)
    INCLUDE (JobID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobIndexPostingsExtended_Published' AND object_id = OBJECT_ID('dbo.JobIndexPostingsExtended'))
BEGIN
    CREATE INDEX IX_JobIndexPostingsExtended_Published
    ON dbo.JobIndexPostingsExtended(Published)
    INCLUDE (JobID);
END
GO

-- Categories name index to speed up LIKE on category name
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_Name' AND object_id = OBJECT_ID('dbo.Categories'))
BEGIN
    CREATE INDEX IX_Categories_Name
    ON dbo.Categories(Name);
END
GO

-- JobCategories support index for joins (PK exists on (JobID, CategoryID))
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobCategories_CategoryID' AND object_id = OBJECT_ID('dbo.JobCategories'))
BEGIN
    CREATE INDEX IX_JobCategories_CategoryID
    ON dbo.JobCategories(CategoryID);
END
GO

-- JobKeywords indexes used by recommendation query
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobKeywords_Keyword' AND object_id = OBJECT_ID('dbo.JobKeywords'))
BEGIN
    CREATE INDEX IX_JobKeywords_Keyword
    ON dbo.JobKeywords(Keyword)
    INCLUDE (JobID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobKeywords_JobID_Keyword' AND object_id = OBJECT_ID('dbo.JobKeywords'))
BEGIN
    CREATE UNIQUE INDEX IX_JobKeywords_JobID_Keyword
    ON dbo.JobKeywords(JobID, Keyword);
END
GO
