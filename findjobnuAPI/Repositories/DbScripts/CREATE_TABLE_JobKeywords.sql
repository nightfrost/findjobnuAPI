USE [JobScraperDB]
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='JobKeywords' and xtype='U')
CREATE TABLE [dbo].[JobKeywords](
	[KeywordID] [int] IDENTITY(1,1) NOT NULL,
	[JobID] [int] NOT NULL,
	[Keyword] [nvarchar](255) NOT NULL,
	[Source] [nvarchar](50) NULL, -- e.g., 'JobDescription_DB', 'JobDescription_URL', 'PDF_URL'
	[ConfidenceScore] [float] NULL,
PRIMARY KEY CLUSTERED
(
	[KeywordID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[JobKeywords]  WITH CHECK ADD  CONSTRAINT [FK_JobKeywords_JobIndexPostings] FOREIGN KEY([JobID])
REFERENCES [dbo].[JobIndexPostings] ([JobID])
ON DELETE CASCADE -- If a job is deleted, its keywords are also deleted
GO

ALTER TABLE [dbo].[JobKeywords] CHECK CONSTRAINT [FK_JobKeywords_JobIndexPostings]
GO

-- Optional: Add a unique constraint to prevent duplicate keywords for the same JobID
-- This might be useful if you only want each unique keyword once per job
ALTER TABLE [dbo].[JobKeywords] ADD CONSTRAINT UQ_JobId_Keyword UNIQUE (JobID, Keyword);
GO